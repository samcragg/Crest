// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Allows the generation of classes for serializing enumerations directly
    /// to the response stream.
    /// </summary>
    internal sealed class EnumSerializerGenerator : TypeSerializerGenerator
    {
        private readonly MethodInfo arrayGetLength;
        private readonly MethodInfo beginWriteMethod;
        private readonly MethodInfo endWriteMethod;
        private readonly MethodInfo getWriterMethod;
        private readonly MethodInfo iListGetItem;
        private readonly MethodInfo objectToString;

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
            this.arrayGetLength = typeof(Array)
                .GetProperty(nameof(Array.Length))
                .GetGetMethod();

            Type primitiveSerializer = GetGenericInterfaceImplementation(
                baseClass.GetTypeInfo(),
                typeof(IPrimitiveSerializer<>));

            this.beginWriteMethod = primitiveSerializer
                .GetMethod(nameof(IPrimitiveSerializer<object>.BeginWrite));

            this.endWriteMethod = primitiveSerializer
                .GetMethod(nameof(IPrimitiveSerializer<object>.EndWrite));

            this.getWriterMethod = primitiveSerializer
                .GetProperty(nameof(IPrimitiveSerializer<object>.Writer))
                .GetGetMethod();

            this.iListGetItem = typeof(IList)
                .GetProperty("Item")
                .GetGetMethod();

            this.objectToString = typeof(object)
                .GetMethod(nameof(object.ToString));
        }

        /// <summary>
        /// Generates a serializer for writing enumeration values as strings.
        /// </summary>
        /// <returns>The generated type of the serializer.</returns>
        public Type GenerateStringSerializer()
        {
            TypeBuilder builder = this.CreateType(nameof(Enum));
            this.EmitConstructor(builder, typeof(Stream));
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
            this.EmitConstructor(builder, typeof(Stream));
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

            var arrayEmitter = new ArraySerializeEmitter(generator, this.BaseClass)
            {
                LoadArray = g => g.EmitLoadArgument(1),
                LoadArrayElement = (g, _) => g.EmitCall(OpCodes.Callvirt, this.iListGetItem, null),
                LoadArrayLength = g => g.EmitCall(OpCodes.Callvirt, this.arrayGetLength, null),
                LoopCounterLocalIndex = 0,
                WriteValue = (_, loadElement) =>
                {
                    generator.EmitLoadArgument(0);
                    generator.EmitCall(this.BaseClass, this.getWriterMethod);
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
                this.StreamWriterMethods[underlyingType],
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
                    typeof(IStreamWriter),
                    this.StreamWriterMethods[underlyingType]);
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
                this.beginWriteMethod,
                typeof(Enum));

            // this.Writer.WriteXXX((XXX)parameter)
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.getWriterMethod);
            loadValue(generator);
            generator.EmitCall(writeMethod.DeclaringType, writeMethod);

            // thie.EndWrite()
            generator.EmitLoadArgument(0);
            generator.EmitCall(this.BaseClass, this.endWriteMethod);
            generator.Emit(OpCodes.Ret);
        }

        private void EmitWriteStringValues(TypeBuilder builder)
        {
            this.EmitWriteMethod(
                builder,
                this.StreamWriterMethods[typeof(string)],
                g =>
                {
                    // No need for null checking as that's done higher up in the
                    // pipeline.
                    //
                    // arg.ToString();
                    g.EmitLoadArgument(1);
                    g.EmitCall(OpCodes.Callvirt, this.objectToString, null);
                });

            this.EmitWriteArrayMethod(builder, g =>
            {
                g.EmitCall(OpCodes.Callvirt, this.objectToString, null);
                g.EmitCall(
                    typeof(IStreamWriter),
                    this.StreamWriterMethods[typeof(string)]);
            });
        }
    }
}
