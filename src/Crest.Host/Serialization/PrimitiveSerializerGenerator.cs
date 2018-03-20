// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the generation of classes for serializing primitive types
    /// directly to the response stream.
    /// </summary>
    internal sealed class PrimitiveSerializerGenerator : TypeSerializerGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSerializerGenerator"/> class.
        /// </summary>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        public PrimitiveSerializerGenerator(ModuleBuilder module, Type baseClass)
            : base(module, baseClass)
        {
        }

        /// <summary>
        /// Generates the serializers for the various built in types handled
        /// by <see cref="ValueWriter"/>.
        /// </summary>
        /// <returns>
        /// A sequence of key value pairs, with the key representing the type
        /// that can be serialized and the value representing the type of the
        /// serializer.
        /// </returns>
        public IEnumerable<KeyValuePair<Type, Type>> GetSerializers()
        {
            foreach (KeyValuePair<Type, MethodInfo> kvp in this.Methods.ValueWriter)
            {
                // Create the class for writing the primitive type first
                Type primitive = kvp.Key;
                Type serializer = this.GenerateType(
                    primitive.Name,
                    primitive,
                    kvp.Value);

                yield return new KeyValuePair<Type, Type>(primitive, serializer);

                // Now see if we need to handle nullable versions (i.e. a
                // serializer for int and int?)
                if (primitive.GetTypeInfo().IsValueType)
                {
                    Type nullable = typeof(Nullable<>).MakeGenericType(primitive);
                    serializer = this.GenerateType(
                        primitive.Name + "?",
                        nullable,
                        kvp.Value);

                    yield return new KeyValuePair<Type, Type>(nullable, serializer);
                }
            }
        }

        /// <summary>
        /// Emits code to read a value from the stream.
        /// </summary>
        /// <param name="generator">Where to emit the code to.</param>
        /// <param name="baseClass">The type of the base class.</param>
        /// <param name="methods">Contains the methods metadata.</param>
        /// <param name="type">The type of the value to read.</param>
        /// <param name="readValue">
        /// Used to emit code to read the primitive value.
        /// </param>
        internal static void EmitReadValue(
            ILGenerator generator,
            Type baseClass,
            Methods methods,
            Type type,
            Action<ILGenerator, Type> readValue)
        {
            Label notNull = generator.DefineLabel();
            Label end = generator.DefineLabel();

            if (CanBeNull(type))
            {
                // if (this.Reader.ReadNull())
                generator.EmitLoadArgument(0);
                generator.EmitCall(baseClass, methods.PrimitiveSerializer.GetReader);
                generator.EmitCall(typeof(ValueReader), methods.ValueReader.ReadNull);
                generator.Emit(OpCodes.Brfalse_S, notNull);

                // object result = null
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Br_S, end);
            }

            // this.Reader.ReadXXX();
            generator.MarkLabel(notNull);
            readValue(generator, Nullable.GetUnderlyingType(type) ?? type);
            generator.MarkLabel(end);
        }

        private void EmitWriteArrayMethod(TypeBuilder builder, MethodInfo writeMethod, Type type)
        {
            MethodBuilder methodBuilder = builder.DefineMethod(
                nameof(ITypeSerializer.WriteArray),
                PublicVirtualMethod,
                CallingConventions.HasThis);

            methodBuilder.SetParameters(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass, this.Methods)
            {
                WriteValue = (_, loadElement) =>
                {
                    generator.EmitLoadArgument(0);
                    generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetWriter);
                    loadElement(generator); // This handles nullable types for us
                    generator.EmitCall(writeMethod.DeclaringType, writeMethod);
                }
            };

            generator.EmitLoadArgument(1); // 0 = this, 1 = array
            arrayEmitter.EmitWriteArray(type.MakeArrayType());
            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteMethod(TypeBuilder builder, MethodInfo writeMethod, Type type)
        {
            Type primitive = Nullable.GetUnderlyingType(type) ?? type;
            MethodBuilder methodBuilder = builder.DefineMethod(
                nameof(ITypeSerializer.Write),
                PublicVirtualMethod,
                CallingConventions.HasThis);

            methodBuilder.SetParameters(typeof(object));
            ILGenerator generator = methodBuilder.GetILGenerator();

            // this.BeginWrite(metadata)
            this.EmitCallBeginMethodWithTypeMetadata(
                builder,
                generator,
                this.Methods.PrimitiveSerializer.BeginWrite,
                type);

            // this.Writer.WriteXXX((XXX)parameter)
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetWriter);
            generator.EmitLoadArgument(1); // 0 = this, 1 = object

            // No need to check if it's a value or reference type (it will be a
            // reference type for string), as unbox_any "applied to a reference
            // type [...] has the same effect as castclass"
            // Also, nullable types are boxed as their underlying value only (if
            // they have one) - i.e. int? -> boxed int
            generator.Emit(OpCodes.Unbox_Any, primitive);
            generator.EmitCall(writeMethod.DeclaringType, writeMethod);

            // this.EndWrite()
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.EndWrite);
            generator.Emit(OpCodes.Ret);
        }

        private Type GenerateType(string name, Type type, MethodInfo writeMethod)
        {
            TypeBuilder builder = this.CreateType(name);
            this.EmitConstructor(builder, null, typeof(Stream), typeof(SerializationMode));
            this.EmitWriteMethod(builder, writeMethod, type);
            this.EmitWriteArrayMethod(builder, writeMethod, type);
            return this.GenerateType(builder, type);
        }
    }
}
