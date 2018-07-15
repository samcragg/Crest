// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization.Internal;

    /// <content>
    /// Contains the nested <see cref="WriteMethodEmitter"/> class.
    /// </content>
    internal sealed partial class ClassSerializerGenerator
    {
        private class WriteMethodEmitter
        {
            private readonly TypeSerializerBuilder builder;

            private readonly Dictionary<Type, LocalBuilder> locals
                = new Dictionary<Type, LocalBuilder>();

            private readonly IReadOnlyDictionary<string, FieldInfo> metadataFields;

            private readonly Dictionary<Type, FieldBuilder> nestedSerializersFields
                = new Dictionary<Type, FieldBuilder>();

            private readonly ClassSerializerGenerator owner;
            private readonly bool writeEnumNames;
            private ILGenerator generator;

            public WriteMethodEmitter(
                ClassSerializerGenerator owner,
                TypeSerializerBuilder builder,
                IReadOnlyDictionary<string, FieldInfo> metadataFields)
            {
                this.builder = builder;
                this.metadataFields = metadataFields;
                this.owner = owner;
                this.writeEnumNames = SerializerGenerator.OutputEnumNames(owner.BaseClass);
            }

            public MethodInfo GeneratedMethod { get; private set; }

            public IReadOnlyDictionary<Type, FieldBuilder> NestedSerializerFields
                => this.nestedSerializersFields;

            public void WriteProperties(IEnumerable<PropertyInfo> properties)
            {
                this.generator = this.CreateWriteMethod();

                // T instance = (T)parameter
                this.generator.DeclareLocal(this.builder.SerializedType);
                this.generator.EmitLoadArgument(1); // Argument 0 is 'this', 1 is the value
                this.generator.Emit(OpCodes.Castclass, this.builder.SerializedType);
                this.generator.EmitStoreLocal(0);

                // base.WriteBeginClass(metadata or null)
                this.owner.EmitCallBeginMethodWithTypeMetadata(
                    this.builder,
                    this.generator,
                    this.owner.Methods.BaseClass.WriteBeginClass);

                foreach (PropertyInfo property in properties)
                {
                    this.EmitWriteProperty(property);
                }

                // base.WriteEndClass()
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.BaseClass.WriteEndClass);
                this.generator.Emit(OpCodes.Ret);
            }

            private ILGenerator CreateWriteMethod()
            {
                MethodBuilder methodBuilder = this.builder.CreatePublicVirtualMethod(
                    nameof(ITypeSerializer.Write));

                methodBuilder.SetParameters(typeof(object));
                this.GeneratedMethod = methodBuilder;
                return methodBuilder.GetILGenerator();
            }

            private void EmitCallWriteMethod(Type type)
            {
                // Try to find a specific method. The caller has already done
                // the check for nullables and passed the underlying type.
                TypeInfo typeInfo = type.GetTypeInfo();
                if (typeInfo.IsEnum)
                {
                    if (this.writeEnumNames)
                    {
                        // ((object)e).ToString()
                        this.generator.Emit(OpCodes.Box, type);
                        this.generator.EmitCall(
                            OpCodes.Callvirt,
                            typeof(object).GetMethod(nameof(object.ToString)),
                            null);

                        type = typeof(string);
                    }
                    else
                    {
                        type = typeInfo.GetEnumUnderlyingType();
                    }
                }

                if (this.owner.Methods.ValueWriter.TryGetMethod(type, out MethodInfo method))
                {
                    this.generator.EmitCall(typeof(ValueWriter), method);
                }
                else
                {
                    // Fall back to the generic object one
                    this.generator.EmitConvertToObject(type);
                    this.generator.EmitCall(typeof(ValueWriter), this.owner.Methods.ValueWriter.WriteObject);
                }
            }

            private void EmitLoadPropertyValue(PropertyInfo property)
            {
                this.generator.EmitLoadLocal(0);
                this.generator.EmitCall(property.DeclaringType, property.GetGetMethod());
            }

            private void EmitNullCheck(Type type, Label end)
            {
                // We need to emit different code for nullable types
                Type nullable = Nullable.GetUnderlyingType(type);
                if (nullable != null)
                {
                    // We need to store nullables in a local variable so that
                    // we can invoke the methods by passing a reference to the
                    // variable (i.e. the this pointer points to a local)
                    LocalBuilder local = this.GetOrAddLocal(type);
                    this.generator.EmitStoreLocal(local.LocalIndex);
                    this.generator.Emit(OpCodes.Ldloca_S, local.LocalIndex);

                    MethodInfo getHasValue =
                        type.GetProperty(nameof(Nullable<int>.HasValue))
                            .GetGetMethod();
                    this.generator.EmitCall(OpCodes.Call, getHasValue, null);
                }

                // Works for nulls or booleans
                this.generator.Emit(OpCodes.Brfalse, end);
            }

            private void EmitSerializeValue(Type type, Action<ILGenerator> loadValue)
            {
                // TypeConverter allows all classes to be converted to a string
                // (they have the ToString method), however, CanConvertFrom
                // returns false by default unless the class has a custom type
                // descriptor that actually checks for it. Use this to see if
                // we're serializing a straight forward type or if we need to
                // use a nested serializer for it.
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    this.EmitWriterMethodCall(type, loadValue);
                }
                else
                {
                    // Use a specialist serializer to serialize the value
                    FieldBuilder serializer = this.GetOrCreateNestedSerializer(type);
                    MethodInfo writeMethod =
                        typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Write));

                    // this.serializer.Write(value)
                    this.generator.EmitLoadArgument(0);
                    this.generator.Emit(OpCodes.Ldfld, serializer);
                    loadValue(this.generator);
                    this.generator.EmitCall(serializer.FieldType, writeMethod);
                }
            }

            private void EmitWriteArray(PropertyInfo property)
            {
                var arrayEmitter = new ArraySerializeEmitter(this.generator, this.owner.BaseClass, this.owner.Methods)
                {
                    WriteValue = this.EmitSerializeValue,
                };

                // var array = this.ArrayProperty
                this.EmitLoadPropertyValue(property);
                arrayEmitter.EmitWriteArray(property.PropertyType);
            }

            private void EmitWriteBeginProperty(string propertyName)
            {
                this.generator.EmitLoadArgument(0);
                this.generator.Emit(OpCodes.Ldsfld, this.metadataFields[propertyName]);
                this.generator.EmitCall(
                    this.owner.BaseClass,
                    this.owner.Methods.BaseClass.WriteBeginProperty);
            }

            private void EmitWriteProperty(PropertyInfo property)
            {
                // There's no cost to creating labels so create and mark it
                // just in case we need to jump as a result of the null check
                Label end = this.generator.DefineLabel();

                if (CanBeNull(property.PropertyType))
                {
                    // Load the value from the property we're about to check (the
                    // cast version is stored in the first local)
                    this.EmitLoadPropertyValue(property);
                    this.EmitNullCheck(property.PropertyType, end);
                }

                this.EmitWriteBeginProperty(property.Name);
                if (property.PropertyType.IsArray)
                {
                    this.EmitWriteArray(property);
                }
                else
                {
                    this.EmitSerializeValue(
                        property.PropertyType,
                        _ => this.EmitLoadPropertyValue(property));
                }

                // this.WriteEndProperty()
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(
                    this.owner.BaseClass,
                    this.owner.Methods.BaseClass.WriteEndProperty);

                this.generator.MarkLabel(end);
            }

            private void EmitWriterMethodCall(Type type, Action<ILGenerator> loadValue)
            {
                // this.Writer.WriteXXX(value)
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.PrimitiveSerializer.GetWriter);
                loadValue(this.generator);

                Type underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                {
                    // value.GetValueOrDefault()
                    // See comments in EmitNullCheck - to invoke a method on
                    // a value type you need to put a pointer to it on the stack
                    LocalBuilder local = this.GetOrAddLocal(type);
                    this.generator.EmitStoreLocal(local.LocalIndex);
                    this.generator.Emit(OpCodes.Ldloca_S, local.LocalIndex);

                    MethodInfo getValueOrDefault = type.GetMethod(
                        nameof(Nullable<int>.GetValueOrDefault),
                        Type.EmptyTypes);

                    this.generator.EmitCall(OpCodes.Call, getValueOrDefault, null);
                    this.EmitCallWriteMethod(underlyingType);
                }
                else
                {
                    this.EmitCallWriteMethod(type);
                }
            }

            private LocalBuilder GetOrAddLocal(Type type)
            {
                if (!this.locals.TryGetValue(type, out LocalBuilder local))
                {
                    local = this.generator.DeclareLocal(type);
                    this.locals.Add(type, local);
                }

                return local;
            }

            private FieldBuilder GetOrCreateNestedSerializer(Type propertyType)
            {
                if (!this.nestedSerializersFields.TryGetValue(propertyType, out FieldBuilder field))
                {
                    Type serializerType = this.owner.generateSerializer(propertyType);
                    field = this.builder.Builder.DefineField(
                        serializerType.Name,
                        serializerType,
                        FieldAttributes.Private | FieldAttributes.InitOnly);

                    this.nestedSerializersFields.Add(propertyType, field);
                }

                return field;
            }
        }
    }
}
