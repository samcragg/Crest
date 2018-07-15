// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the generation of classes for serializing enumerations directly
    /// to the response stream.
    /// </summary>
    internal sealed class EnumSerializerGenerator : TypeSerializerGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSerializerGenerator"/> class.
        /// </summary>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        public EnumSerializerGenerator(ModuleBuilder module, Type baseClass)
            : base(module, baseClass)
        {
        }

        /// <summary>
        /// Generates a serializer for writing enumeration values as strings.
        /// </summary>
        /// <param name="enumType">The enumeration type.</param>
        /// <returns>The generated type of the serializer.</returns>
        public Type GenerateStringSerializer(Type enumType)
        {
            TypeSerializerBuilder builder = this.CreateType(enumType, GetName(enumType));
            this.EmitConstructor(builder, null, typeof(Stream), typeof(SerializationMode));
            this.EmitReadStringValues(builder);
            this.EmitWriteStringValues(builder);
            return builder.GenerateType();
        }

        /// <summary>
        /// Generates a serializer for writing enumeration values directly to
        /// the response stream.
        /// </summary>
        /// <param name="enumType">The enumeration type.</param>
        /// <returns>The generated type of the serializer.</returns>
        public Type GenerateValueSerializer(Type enumType)
        {
            TypeSerializerBuilder builder = this.CreateType(enumType, GetName(enumType));
            this.EmitConstructor(builder, null, typeof(Stream), typeof(SerializationMode));
            this.EmitReadIntegerValues(builder);
            this.EmitWriteIntegerValues(builder);
            return builder.GenerateType();
        }

        /// <summary>
        /// Emits a call to <see cref="ValueReader.ReadString"/> and then
        /// passing the result to <see cref="Enum.Parse(Type, string, bool)"/>
        /// to convert it to an object of the specified type.
        /// </summary>
        /// <param name="baseClass">The type the generated class inherits from.</param>
        /// <param name="methods">Contains method metadata.</param>
        /// <param name="generator">Where to output the generated code.</param>
        /// <param name="enumType">The type of the enum to parse.</param>
        internal static void EmitCallToEnumParse(
            Type baseClass,
            Methods methods,
            ILGenerator generator,
            Type enumType)
        {
            // Enum.Parse(enumType, this.Reader.ReadString(), ignoreCase: true)
            // enumType
            generator.EmitLoadTypeof(enumType);

            // this.Reader.ReadString()
            generator.EmitLoadArgument(0);
            generator.EmitCall(baseClass, methods.PrimitiveSerializer.GetReader);
            generator.EmitCall(typeof(ValueReader), methods.ValueReader[typeof(string)]);

            // ignoreCase: true
            generator.EmitLoadConstant(1);

            // Enum.Parse(...)
            generator.EmitCall(OpCodes.Call, methods.Enum.Parse, null);
        }

        private static string GetName(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return underlyingType.Name + "?";
            }
            else
            {
                return type.Name;
            }
        }

        private void EmitReadArrayMethod(TypeSerializerBuilder builder, Action<ILGenerator, Type> readValue)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                nameof(ITypeSerializer.ReadArray));

            methodBuilder.SetReturnType(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();

            var arrayEmitter = new ArrayDeserializeEmitter(generator, this.BaseClass, this.Methods)
            {
                CreateLocal = generator.DeclareLocal,
                ReadValue = readValue,
            };
            arrayEmitter.EmitReadArray(builder.SerializedType.MakeArrayType());

            generator.Emit(OpCodes.Ret);
        }

        private void EmitReadIntegerValues(TypeSerializerBuilder builder)
        {
            void EmitCallToReadInt(ILGenerator generator, Type type)
            {
                Type underlyingType = Enum.GetUnderlyingType(type);
                MethodInfo readInt = this.Methods.ValueReader[underlyingType];

                // this.Reader.ReadIntXX()
                generator.EmitLoadArgument(0);
                generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetReader);
                generator.EmitCall(typeof(ValueReader), readInt);
            }

            this.EmitReadMethod(
                builder,
                (g, t) =>
                {
                    // Read needs to return an object, so box it
                    EmitCallToReadInt(g, t);
                    g.EmitConvertToObject(t);
                });

            this.EmitReadArrayMethod(builder, EmitCallToReadInt);
        }

        private void EmitReadMethod(TypeSerializerBuilder builder, Action<ILGenerator, Type> readValue)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                nameof(ITypeSerializer.Read));

            methodBuilder.SetReturnType(typeof(object));
            ILGenerator generator = methodBuilder.GetILGenerator();

            // this.BeginRead(metadata)
            this.EmitCallBeginMethodWithTypeMetadata(
                builder,
                generator,
                this.Methods.PrimitiveSerializer.BeginRead);

            // object result = this.reader.ReadXXX()
            PrimitiveSerializerGenerator.EmitReadValue(
                generator,
                this.BaseClass,
                this.Methods,
                builder.SerializedType,
                readValue);

            // this.EndRead()
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.EndRead);
            generator.Emit(OpCodes.Ret);
        }

        private void EmitReadStringValues(TypeSerializerBuilder builder)
        {
            this.EmitReadMethod(
                builder,
                (g, t) => EmitCallToEnumParse(this.BaseClass, this.Methods, g, t));

            this.EmitReadArrayMethod(
                builder,
                (g, t) =>
                {
                    EmitCallToEnumParse(this.BaseClass, this.Methods, g, t);
                    g.Emit(OpCodes.Unbox_Any, t);
                });
        }

        private void EmitWriteArrayMethod(TypeSerializerBuilder builder, Action<ILGenerator> writeValue)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                    nameof(ITypeSerializer.WriteArray));

            methodBuilder.SetParameters(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass, this.Methods)
            {
                WriteValue = (_, loadElement) =>
                {
                    generator.EmitLoadArgument(0);
                    generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetWriter);
                    loadElement(generator);
                    writeValue(generator);
                },
            };

            generator.EmitLoadArgument(1); // 0 = this, 1 = array
            arrayEmitter.EmitWriteArray(builder.SerializedType.MakeArrayType());
            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteIntegerValues(TypeSerializerBuilder builder)
        {
            // Find the primitive the enum inherits from, taking into account
            // that we could have a nullable enum at this stage
            Type underlyingType = Enum.GetUnderlyingType(
                Nullable.GetUnderlyingType(builder.SerializedType) ?? builder.SerializedType);

            this.EmitWriteMethod(
                builder,
                this.Methods.ValueWriter[underlyingType],
                g =>
                {
                    // Nullable values are boxed as their raw value if they
                    // have one (i.e. MyEnum? -> boxed MyEnum). Since we know
                    // it's not null (we wouldn't be invoked if it was as we'd
                    // return 404), no need to worry about whether it's a
                    // nullable enum or not.
                    //
                    // (UnderlyingType)arg
                    g.EmitLoadArgument(1);
                    g.Emit(OpCodes.Unbox_Any, underlyingType);
                });

            this.EmitWriteArrayMethod(builder, g =>
            {
                g.EmitCall(
                    typeof(ValueWriter),
                    this.Methods.ValueWriter[underlyingType]);
            });
        }

        private void EmitWriteMethod(
            TypeSerializerBuilder builder,
            MethodInfo writeMethod,
            Action<ILGenerator> loadValue)
        {
            MethodBuilder methodBuilder = builder.CreatePublicVirtualMethod(
                nameof(ITypeSerializer.Write));

            methodBuilder.SetParameters(typeof(object));
            ILGenerator generator = methodBuilder.GetILGenerator();

            // this.BeginWrite(metadata)
            this.EmitCallBeginMethodWithTypeMetadata(
                builder,
                generator,
                this.Methods.PrimitiveSerializer.BeginWrite);

            // this.Writer.WriteXXX((XXX)parameter)
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetWriter);
            loadValue(generator);
            generator.EmitCall(writeMethod.DeclaringType, writeMethod);

            // thie.EndWrite()
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.EndWrite);
            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteStringValues(TypeSerializerBuilder builder)
        {
            this.EmitWriteMethod(
                builder,
                this.Methods.ValueWriter[typeof(string)],
                g =>
                {
                    // No need for null checking as that's done higher up in the
                    // pipeline and no need for casting as the value is already
                    // boxed into an object.
                    //
                    // arg.ToString()
                    g.EmitLoadArgument(1);
                    g.EmitCall(OpCodes.Callvirt, this.Methods.Object.ToString, null);
                });

            Type underlyingType = Nullable.GetUnderlyingType(builder.SerializedType)
                ?? builder.SerializedType;

            this.EmitWriteArrayMethod(builder, g =>
            {
                // We could store this in a local so we could load the address
                // instead of boxing the value up, reducing GC pressure
                //
                // If the element type is nullable then the value on the stack
                // is the actual value (i.e. it's been unwrapped from the
                // nullable container).
                g.Emit(OpCodes.Box, underlyingType);
                g.EmitCall(OpCodes.Callvirt, this.Methods.Object.ToString, null);
                g.EmitCall(
                    typeof(ValueWriter),
                    this.Methods.ValueWriter[typeof(string)]);
            });
        }
    }
}
