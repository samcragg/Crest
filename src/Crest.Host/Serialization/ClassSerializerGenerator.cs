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
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Generates a class that can serialize a specific type at runtime.
    /// </summary>
    internal sealed partial class ClassSerializerGenerator : TypeSerializerGenerator
    {
        private readonly Func<Type, Type> generateSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassSerializerGenerator"/> class.
        /// </summary>
        /// <param name="generateSerializer">
        /// Used to get additional serializers for nested classes.
        /// </param>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        public ClassSerializerGenerator(Func<Type, Type> generateSerializer, ModuleBuilder module, Type baseClass)
            : base(module, baseClass)
        {
            this.generateSerializer = generateSerializer;
        }

        /// <summary>
        /// Generates a serializer for the specific type.
        /// </summary>
        /// <param name="classType">The class to serialize.</param>
        /// <returns>A type implementing <see cref="ITypeSerializer"/>.</returns>
        public Type GenerateFor(Type classType)
        {
            IReadOnlyList<PropertyInfo> properties = GetProperties(classType);
            TypeSerializerBuilder builder = this.CreateType(classType, classType.Name);

            // Create the methods first, so that if it needs any nested
            // serializers we can initialize them in the constructors
            IEnumerable<FieldBuilder> fields =
                this.EmitMethods(builder, properties);

            // Create the constructors
            this.EmitConstructor(builder, fields, this.BaseClass);
            this.EmitConstructor(builder, fields, typeof(Stream), typeof(SerializationMode));

            return builder.GenerateType();
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

        private void EmitConstructor(
            TypeSerializerBuilder builder,
            IEnumerable<FieldBuilder> nestedSerializers,
            params Type[] parameters)
        {
            this.EmitConstructor(
                builder,
                generator =>
                {
                    foreach (FieldBuilder serializerField in nestedSerializers)
                    {
                        this.EmitInitializeField(generator, serializerField);
                    }
                },
                parameters);
        }

        private void EmitInitializeField(ILGenerator generator, FieldBuilder serializerField)
        {
            bool IsConstructorWithBaseClass(ConstructorInfo constructor)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                return (parameters.Length == 1) && (parameters[0].ParameterType == this.BaseClass);
            }

            // This method generates code to do this:
            //     this.field = new Serializer(this)
            ConstructorInfo serializerConstructor =
                GetCallableConstructors(serializerField.FieldType)
                .Single(IsConstructorWithBaseClass);

            // this.field = ...
            generator.EmitLoadArgument(0);

            // field = new Serializer(this)
            generator.EmitLoadArgument(0);
            generator.Emit(OpCodes.Newobj, serializerConstructor);

            // this.field = ...
            generator.Emit(OpCodes.Stfld, serializerField);
        }

        private IEnumerable<FieldBuilder> EmitMethods(
            TypeSerializerBuilder builder,
            IReadOnlyList<PropertyInfo> properties)
        {
            var writeMethod = new WriteMethodEmitter(this, builder);
            writeMethod.WriteProperties(properties);
            this.EmitWriteArray(builder);

            var readMethod = new ReadMethodEmitter(this, builder, writeMethod.NestedSerializerFields);
            readMethod.EmitReadMethod(GetProperties(builder.SerializedType));
            this.EmitReadArrayMethod(builder);

            return writeMethod.NestedSerializerFields.Values;
        }

        private void EmitReadArrayMethod(TypeSerializerBuilder builder)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                nameof(ITypeSerializer.ReadArray));

            methodBuilder.SetReturnType(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();
            var arrayEmitter = new ArrayDeserializeEmitter(generator, this.BaseClass, this.Methods)
            {
                CreateLocal = generator.DeclareLocal,
                ReadValue = (g, _) =>
                {
                    // The Read method returns an object, so cast it to the
                    // correct type
                    // (T)this.Read()
                    generator.EmitLoadArgument(0);
                    generator.EmitCall(
                        OpCodes.Callvirt,
                        this.Methods.TypeSerializer.Read,
                        null);
                    generator.Emit(OpCodes.Castclass, builder.SerializedType);
                },
            };

            arrayEmitter.EmitReadArray(builder.SerializedType.MakeArrayType());
            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteArray(TypeSerializerBuilder builder)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                    nameof(ITypeSerializer.WriteArray));

            methodBuilder.SetParameters(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass, this.Methods)
            {
                WriteValue = (_, loadElement) =>
                {
                    // Note the null checking has been done by the array emitter
                    // this.Write(array[i])
                    generator.EmitLoadArgument(0);
                    loadElement(generator);
                    generator.EmitCall(
                        OpCodes.Call,
                        this.Methods.TypeSerializer.Write,
                        null);
                },
            };

            generator.EmitLoadArgument(1); // 0 = this, 1 = array
            arrayEmitter.EmitWriteArray(builder.SerializedType.MakeArrayType());
            generator.Emit(OpCodes.Ret);
        }
    }
}
