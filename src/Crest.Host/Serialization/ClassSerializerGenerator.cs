// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    /// <summary>
    /// Generates a class that can serialize a specific type at runtime.
    /// </summary>
    internal sealed partial class ClassSerializerGenerator : TypeSerializerGenerator
    {
        private readonly BaseMethods baseMethods;
        private readonly SerializerGenerator generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassSerializerGenerator"/> class.
        /// </summary>
        /// <param name="generator">
        /// Used to get additional serializers for nested classes.
        /// </param>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        public ClassSerializerGenerator(SerializerGenerator generator, ModuleBuilder module, Type baseClass)
            : base(module, baseClass)
        {
            this.generator = generator;

            Type classSerializerInterface = GetGenericInterfaceImplementation(
                baseClass.GetTypeInfo(),
                typeof(IClassSerializer<>));

            this.baseMethods = new BaseMethods(baseClass, classSerializerInterface);
        }

        /// <summary>
        /// Generates a serializer for the specific type.
        /// </summary>
        /// <param name="classType">The class to serialize.</param>
        /// <returns>A type implementing <see cref="ITypeSerializer"/>.</returns>
        public Type GenerateFor(Type classType)
        {
            IReadOnlyList<PropertyInfo> properties = GetProperties(classType);
            TypeBuilder builder = this.CreateType(classType.Name);

            IReadOnlyDictionary<string, FieldInfo> metadata =
                this.CreateMetadataFields(builder, properties);

            // Create the Write method first, so that if it needs any nested
            // serializers we can initialize them in the constructors
            var writeMethod = new WriteMethodEmitter(this, metadata);
            writeMethod.WriteProperties(builder, classType, properties);
            this.EmitWriteArray(builder, classType, writeMethod.GeneratedMethod);

            // Create the constructors
            this.EmitConstructor(builder, this.BaseClass, writeMethod.NestedSerializerFields);
            this.EmitConstructor(builder, typeof(Stream), writeMethod.NestedSerializerFields);

            // Build the type and set the static metadata
            TypeInfo generatedInfo = builder.CreateTypeInfo();
            Type generatedType = generatedInfo.AsType();
            this.InitializeTypeMetadata(generatedType, classType);
            this.SetMetadataFields(generatedInfo, properties, metadata);
            return generatedType;
        }

        private static IReadOnlyList<PropertyInfo> GetProperties(Type type)
        {
            int DataOrder(PropertyInfo property)
            {
                DataMemberAttribute dataMember = property.GetCustomAttribute<DataMemberAttribute>();
                return (dataMember != null) ? dataMember.Order : int.MaxValue;
            }

            bool IncludeProperty(PropertyInfo property)
            {
                BrowsableAttribute browsable = property.GetCustomAttribute<BrowsableAttribute>();
                if ((browsable != null) && !browsable.Browsable)
                {
                    return false;
                }

                return property.CanRead && property.CanWrite;
            }

            return type.GetProperties()
                       .Where(IncludeProperty)
                       .OrderBy(DataOrder)
                       .ToList();
        }

        private IReadOnlyDictionary<string, FieldInfo> CreateMetadataFields(
            TypeBuilder builder,
            IReadOnlyList<PropertyInfo> properties)
        {
            var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            foreach (PropertyInfo property in properties)
            {
                FieldBuilder field = this.CreateMetadataField(builder, property.Name);
                fields.Add(property.Name, field);
            }

            return fields;
        }

        private void EmitConstructor(
            TypeBuilder builder,
            Type parameter,
            IEnumerable<FieldBuilder> nestedSerializers)
        {
            this.EmitConstructor(builder, parameter, generator =>
            {
                foreach (FieldBuilder serializerField in nestedSerializers)
                {
                    this.EmitInitializeField(generator, serializerField);
                }
            });
        }

        private void EmitInitializeField(ILGenerator generator, FieldBuilder serializerField)
        {
            // This method generates code to do this:
            //     this.field = new Serializer(this)
            ConstructorInfo serializerConstructor =
                serializerField.FieldType.GetConstructor(new[] { this.BaseClass });

            // this.field = ...
            generator.EmitLoadArgument(0);

            // field = new Serializer(this)
            generator.EmitLoadArgument(0);
            generator.Emit(OpCodes.Newobj, serializerConstructor);

            // this.field = ...
            generator.Emit(OpCodes.Stfld, serializerField);
        }

        private void EmitWriteArray(TypeBuilder builder, Type classType, MethodInfo generatedMethod)
        {
            Type arrayType = classType.MakeArrayType();
            MethodBuilder methodBuilder = builder.DefineMethod(
                    nameof(ITypeSerializer.WriteArray),
                    PublicVirtualMethod,
                    CallingConventions.HasThis);

            methodBuilder.SetParameters(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.DeclareLocal(typeof(int)); // loop counter
            generator.DeclareLocal(arrayType);

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass)
            {
                LoadArray = g => g.EmitLoadLocal(1),
                LoopCounterLocalIndex = 0,
                WriteValue = (_, loadElement) =>
                {
                    // Note the null checking has been done by the array emitter
                    // this.Write(array[i])
                    generator.EmitLoadArgument(0);
                    loadElement(generator);
                    generator.EmitCall(
                        OpCodes.Call,
                        typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Write)),
                        null);
                }
            };

            // T[] array = (T[])arg1
            generator.EmitLoadArgument(1);
            generator.Emit(OpCodes.Castclass, arrayType);
            generator.EmitStoreLocal(1);

            arrayEmitter.EmitWriteArray(arrayType);
            generator.Emit(OpCodes.Ret);
        }

        private void SetMetadataFields(
            TypeInfo type,
            IReadOnlyList<PropertyInfo> properties,
            IReadOnlyDictionary<string, FieldInfo> metadataFields)
        {
            foreach (PropertyInfo property in properties)
            {
                // We need to get the real field instead of the builder so that
                // it can be set
                FieldInfo field = type.GetField(
                    metadataFields[property.Name].Name,
                    BindingFlags.Public | BindingFlags.Static);

                object value = this.baseMethods.GetMetadata.Invoke(null, new[] { property });
                field.SetValue(null, value);
            }
        }
    }
}
