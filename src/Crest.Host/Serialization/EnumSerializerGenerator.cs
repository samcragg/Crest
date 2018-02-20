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
        /// <returns>The generated type of the serializer.</returns>
        public Type GenerateStringSerializer()
        {
            TypeBuilder builder = this.CreateType(nameof(Enum));
            this.EmitConstructor(builder, null, typeof(Stream), typeof(SerializationMode));
            this.EmitWriteStringValues(builder);
            return this.GenerateType(builder, typeof(Enum));
        }

        /// <summary>
        /// Generates a serializer for writing enumeration values directly to
        /// the response stream.
        /// </summary>
        /// <param name="underlyingType">The underlying type of the enumeration.</param>
        /// <returns>The generated type of the serializer.</returns>
        public Type GenerateValueSerializer(Type underlyingType)
        {
            TypeBuilder builder = this.CreateType(nameof(Enum) + underlyingType.Name);
            this.EmitConstructor(builder, null, typeof(Stream), typeof(SerializationMode));
            this.EmitWriteIntegerValues(builder, underlyingType);
            return this.GenerateType(builder, typeof(Enum));
        }

        private void EmitWriteArrayMethod(TypeBuilder builder, Action<ILGenerator> writeValue)
        {
            MethodBuilder methodBuilder = builder.DefineMethod(
                nameof(ITypeSerializer.WriteArray),
                PublicVirtualMethod,
                CallingConventions.HasThis);

            methodBuilder.SetParameters(typeof(Array));
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.DeclareLocal(typeof(int));

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass, this.Methods)
            {
                LoadArray = g => g.EmitLoadArgument(1),
                LoadArrayElement = (g, _) => g.EmitCall(OpCodes.Callvirt, this.Methods.List.GetItem, null),
                LoadArrayLength = g => g.EmitCall(OpCodes.Callvirt, this.Methods.List.GetCount, null),
                LoopCounterLocalIndex = 0,
                WriteValue = (_, loadElement) =>
                {
                    generator.EmitLoadArgument(0);
                    generator.EmitCall(this.BaseClass, this.Methods.PrimitiveSerializer.GetWriter);
                    loadElement(generator);
                    writeValue(generator);
                }
            };
            arrayEmitter.EmitWriteArray(typeof(Enum[]));

            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteIntegerValues(TypeBuilder builder, Type underlyingType)
        {
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
                    // (UnderlyingType)arg;
                    g.EmitLoadArgument(1);
                    g.Emit(OpCodes.Unbox_Any, underlyingType);
                });

            this.EmitWriteArrayMethod(builder, g =>
            {
                // We've got an object on the stack (IList.get_Item) so unbox it
                g.Emit(OpCodes.Unbox_Any, underlyingType);
                g.EmitCall(
                    typeof(ValueWriter),
                    this.Methods.ValueWriter[underlyingType]);
            });
        }

        private void EmitWriteMethod(
            TypeBuilder builder,
            MethodInfo writeMethod,
            Action<ILGenerator> loadValue)
        {
            MethodBuilder methodBuilder = builder.DefineMethod(
                nameof(ITypeSerializer.Write),
                PublicVirtualMethod,
                CallingConventions.HasThis);

            methodBuilder.SetParameters(typeof(object));
            ILGenerator generator = methodBuilder.GetILGenerator();

            // this.BeginWrite(metadata)
            this.EmitWriteBeginTypeMetadata(
                builder,
                generator,
                this.Methods.PrimitiveSerializer.BeginWrite,
                typeof(Enum));

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

        private void EmitWriteStringValues(TypeBuilder builder)
        {
            this.EmitWriteMethod(
                builder,
                this.Methods.ValueWriter[typeof(string)],
                g =>
                {
                    // No need for null checking as that's done higher up in the
                    // pipeline.
                    //
                    // arg.ToString();
                    g.EmitLoadArgument(1);
                    g.EmitCall(OpCodes.Callvirt, this.Methods.Object.ToString, null);
                });

            this.EmitWriteArrayMethod(builder, g =>
            {
                g.EmitCall(OpCodes.Callvirt, this.Methods.Object.ToString, null);
                g.EmitCall(
                    typeof(ValueWriter),
                    this.Methods.ValueWriter[typeof(string)]);
            });
        }
    }
}
