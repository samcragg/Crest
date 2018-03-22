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
    /// Contains the nested <see cref="ReadMethodEmitter"/> class.
    /// </content>
    internal sealed partial class ClassSerializerGenerator
    {
        private class ReadMethodEmitter
        {
            private readonly TypeSerializerBuilder builder;

            private readonly Dictionary<Type, LocalBuilder> locals
                = new Dictionary<Type, LocalBuilder>();

            private readonly IReadOnlyDictionary<Type, FieldBuilder> nestedSerializerFields;
            private readonly ClassSerializerGenerator owner;
            private readonly bool useEnumNames;
            private ILGenerator generator;

            public ReadMethodEmitter(
                ClassSerializerGenerator owner,
                TypeSerializerBuilder builder,
                IReadOnlyDictionary<Type, FieldBuilder> nestedSerializerFields)
            {
                this.builder = builder;
                this.nestedSerializerFields = nestedSerializerFields;
                this.owner = owner;
                this.useEnumNames = SerializerGenerator.OutputEnumNames(this.owner.BaseClass);
            }

            public void EmitReadMethod(IReadOnlyList<PropertyInfo> properties)
            {
                MethodBuilder method = this.builder.CreatePublicVirtualMethod(
                    nameof(ITypeSerializer.Read));

                method.SetReturnType(typeof(object));
                this.generator = method.GetILGenerator();
                int instanceIndex = this.generator.DeclareLocal(this.builder.SerializedType).LocalIndex;

                // T instance = new T();
                InitializeInstance(this.generator, this.builder.SerializedType, instanceIndex);

                // this.BeginReadClass(metadata or null);
                this.owner.EmitCallBeginMethodWithTypeMetadata(
                    this.builder,
                    this.generator,
                    this.owner.Methods.BaseClass.ReadBeginClass);

                // instance.Property = this.Reader.ReadXXX()
                this.EmitReadProperties(properties, instanceIndex);

                // this.EndReadClass();
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.BaseClass.ReadEndClass);

                // Because we're only deserializing reference types, there's no
                // need to box the instance variable.
                //
                // return instance;
                this.generator.EmitLoadLocal(instanceIndex);
                this.generator.Emit(OpCodes.Ret);
            }

            private static string GetPropertyName(PropertyInfo property)
            {
                DisplayNameAttribute displayName =
                    property.GetCustomAttribute<DisplayNameAttribute>();

                string name = displayName?.DisplayName ?? property.Name;
                return name.ToUpperInvariant();
            }

            private static void InitializeInstance(ILGenerator generator, Type classType, int localIndex)
            {
                ConstructorInfo constructor =
                    classType.GetConstructor(Type.EmptyTypes) ??
                    throw new InvalidOperationException(classType.Name + " must contain a public default constructor");

                // T instance = new T();
                generator.Emit(OpCodes.Newobj, constructor);
                generator.EmitStoreLocal(localIndex);
            }

            private void EmitJumptTable(IEnumerable<PropertyInfo> properties, int instanceIndex, int nameIndex, Label endOfLoop)
            {
                var jumpTable = new JumpTableGenerator(CaseInsensitiveStringHelper.GetHashCode)
                {
                    EmitCondition = (g, k) =>
                    {
                        g.Emit(OpCodes.Ldstr, k);
                        g.EmitLoadLocal(nameIndex);
                        g.EmitCall(OpCodes.Call, this.owner.Methods.CaseInsensitiveStringHelper.Equals, null);
                    },
                    EmitGetHashCode = g =>
                    {
                        g.EmitLoadLocal(nameIndex);
                        g.EmitCall(OpCodes.Call, this.owner.Methods.CaseInsensitiveStringHelper.GetHashCode, null);
                    },
                    EndOfTable = endOfLoop,
                    NoMatch = g => { }
                };

                foreach (PropertyInfo property in properties)
                {
                    this.EmitReadProperty(instanceIndex, property, jumpTable);
                }

                jumpTable.EmitJumpTable(this.generator);
            }

            private void EmitReadArrayValue(Type arrayType)
            {
                var arrayEmitter = new ArrayDeserializeEmitter(this.generator, this.owner.BaseClass, this.owner.Methods)
                {
                    CreateLocal = t => this.GetOrAddLocal(t)
                };

                arrayEmitter.ReadValue = (_, t) => this.EmitReadValue(t);
                arrayEmitter.EmitReadArray(arrayType);
            }

            private void EmitReadBeginProperty(int nameIndex, Label startOfLoop)
            {
                // name = this.ReadBeginProperty()
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.BaseClass.ReadBeginProperty);
                this.generator.Emit(OpCodes.Dup);
                this.generator.EmitStoreLocal(nameIndex);

                // if (name != null) goto start
                this.generator.Emit(OpCodes.Brtrue, startOfLoop);
            }

            private void EmitReadEnumValue(Type enumType)
            {
                if (this.useEnumNames)
                {
                    EnumSerializerGenerator.EmitCallToEnumParse(
                        this.owner.BaseClass,
                        this.owner.Methods,
                        this.generator,
                        enumType);

                    this.generator.Emit(OpCodes.Unbox_Any, enumType);
                }
                else
                {
                    this.EmitReaderMethodCall(Enum.GetUnderlyingType(enumType));
                }
            }

            private void EmitReaderMethodCall(Type type)
            {
                if (type.GetTypeInfo().IsEnum)
                {
                    this.EmitReadEnumValue(type);
                }
                else
                {
                    // this.Reader.ReadXXX
                    this.generator.EmitLoadArgument(0);
                    this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.PrimitiveSerializer.GetReader);

                    if (this.owner.Methods.ValueReader.TryGetMethod(type, out MethodInfo method))
                    {
                        this.generator.EmitCall(typeof(ValueReader), method);
                    }
                    else
                    {
                        // Fall back to the generic object one
                        // (T)this.Reader.ReadObject(type)
                        this.generator.EmitLoadTypeof(type);
                        this.generator.EmitCall(typeof(ValueReader), this.owner.Methods.ValueReader.ReadObject);
                        this.generator.Emit(OpCodes.Unbox_Any, type); // Handles value and reference types
                    }
                }
            }

            private void EmitReadNullableValue(Type nullableType, Type underlyingType)
            {
                Label elseLabel = this.generator.DefineLabel();
                Label endLabel = this.generator.DefineLabel();

                // if (reader.ReadNull())
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.PrimitiveSerializer.GetReader);
                this.generator.EmitCall(typeof(ValueReader), this.owner.Methods.ValueReader.ReadNull);
                this.generator.Emit(OpCodes.Brfalse_S, elseLabel);

                // We're setting the property in the caller, so just load the
                // default value for a nullable<T> on the stack
                //    default(Nullable<T>)
                LocalBuilder nullable = this.GetOrAddLocal(nullableType);
                this.generator.Emit(OpCodes.Ldloca_S, nullable.LocalIndex);
                this.generator.Emit(OpCodes.Initobj, nullableType);
                this.generator.EmitLoadLocal(nullable.LocalIndex);
                this.generator.Emit(OpCodes.Br_S, endLabel);

                // else
                //    new Nullable<T>(x)
                this.generator.MarkLabel(elseLabel);
                this.EmitReadValue(underlyingType);
                this.generator.Emit(OpCodes.Newobj, nullableType.GetConstructor(new[] { underlyingType }));

                // end if
                this.generator.MarkLabel(endLabel);
            }

            private void EmitReadProperties(IReadOnlyList<PropertyInfo> properties, int instanceIndex)
            {
                int nameIndex = this.generator.DeclareLocal(typeof(string)).LocalIndex;

                // string = this.ReadBeginProperty()
                Label readNextProperty = this.generator.DefineLabel();
                this.generator.Emit(OpCodes.Br, readNextProperty);

                // switch (name) { ... }
                Label startOfLoop = this.generator.DefineLabel();
                Label endOfSwitch = this.generator.DefineLabel();
                this.generator.MarkLabel(startOfLoop);
                this.EmitJumptTable(properties, instanceIndex, nameIndex, endOfSwitch);

                // this.ReadEndProperty()
                this.generator.MarkLabel(endOfSwitch);
                this.generator.EmitLoadArgument(0);
                this.generator.EmitCall(this.owner.BaseClass, this.owner.Methods.BaseClass.ReadEndProperty);

                // while (name != null)
                this.generator.MarkLabel(readNextProperty);
                this.EmitReadBeginProperty(nameIndex, startOfLoop);
            }

            private void EmitReadProperty(int instanceIndex, PropertyInfo property, JumpTableGenerator jumpTable)
            {
                jumpTable.Map(GetPropertyName(property), g =>
                {
                    // instance.Property = ReadValue()
                    g.EmitLoadLocal(instanceIndex);

                    if (property.PropertyType.IsArray)
                    {
                        this.EmitReadArrayValue(property.PropertyType);
                    }
                    else
                    {
                        Type underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                        if (underlyingType != null)
                        {
                            this.EmitReadNullableValue(property.PropertyType, underlyingType);
                        }
                        else
                        {
                            this.EmitReadValue(property.PropertyType);
                        }
                    }

                    g.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
                });
            }

            private void EmitReadValue(Type type)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    this.EmitReaderMethodCall(type);
                }
                else
                {
                    // Use a specialist serializer to serialize the value
                    FieldBuilder serializer = this.nestedSerializerFields[type];
                    MethodInfo readMethod =
                        typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Read));

                    // (T)this.serializer.Read()
                    this.generator.EmitLoadArgument(0);
                    this.generator.Emit(OpCodes.Ldfld, serializer);
                    this.generator.EmitCall(serializer.FieldType, readMethod);
                    this.generator.Emit(OpCodes.Unbox_Any, type);
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
        }
    }
}
