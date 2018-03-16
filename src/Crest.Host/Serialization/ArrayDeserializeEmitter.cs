// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Emits the code for deserializing an array of items.
    /// </summary>
    internal sealed class ArrayDeserializeEmitter
    {
        // This class generates the following code to read an array:
        //
        //     ArrayBuffer<int> buffer = default;
        //     if (this.ReadBeginArray())
        //     {
        //         do
        //         {
        //             if (this.Reader.ReadNull())
        //             {
        //                 buffer.Add(null);
        //             }
        //             else
        //             {
        //                 buffer.Add(this.Read());
        //             }
        //         }
        //         while (this.ReadElementSeparator());
        //
        //         this.ReadEndArray();
        //     }
        //
        //     T[] array = buffer.ToArray();
        private readonly Type baseClass;
        private readonly ILGenerator generator;
        private readonly Methods methods;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayDeserializeEmitter"/> class.
        /// </summary>
        /// <param name="generator">Where to emit the code to.</param>
        /// <param name="baseClass">The type of the base serializer class.</param>
        /// <param name="methods">Contains the method metadata.</param>
        public ArrayDeserializeEmitter(ILGenerator generator, Type baseClass, Methods methods)
        {
            this.baseClass = baseClass;
            this.generator = generator;
            this.methods = methods;
        }

        /// <summary>
        /// Gets or sets the action to call to create a local variable.
        /// </summary>
        public Func<Type, LocalBuilder> CreateLocal { get; set; }

        /// <summary>
        /// Gets or sets the action to call to read the value from the input
        /// stream.
        /// </summary>
        /// <remarks>
        /// The argument is the type of the value being read. When this method
        /// returns it is expected the value has been loaded onto the
        /// evaluation stack as the correct type (i.e. not boxed)
        /// </remarks>
        public Action<ILGenerator, Type> ReadValue { get; set; }

        /// <summary>
        /// Emits the code to deserialize all the elements of an array.
        /// </summary>
        /// <param name="arrayType">The type of the array.</param>
        /// <remarks>
        /// The read array will be left on the evaluation stack.
        /// </remarks>
        public void EmitReadArray(Type arrayType)
        {
            Type elementType = arrayType.GetElementType();
            Type arrayBufferType = typeof(ArrayBuffer<>).MakeGenericType(elementType);
            LocalBuilder arrayBuffer = this.CreateLocal(arrayBufferType);

            // ArrayBuffer<elementType> buffer = default;
            this.generator.Emit(OpCodes.Ldloca_S, arrayBuffer.LocalIndex);
            this.generator.Emit(OpCodes.Initobj, arrayBufferType);

            // if (this.ReadBeginArray(elementType)
            Label endIf = this.EmitCallReadBeginArray(elementType);

            // do {
            Label loopStart = this.generator.DefineLabel();
            this.generator.MarkLabel(loopStart);

            // buffer.Add(ReadElement(...));
            this.EmitReadElement(arrayBufferType, arrayBuffer.LocalIndex, elementType);

            // } while (this.ReadElementSeparator());
            this.EmitReadElementSeparator(loopStart);

            // this.ReadEndArray();
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.ArraySerializer.ReadEndArray);

            // T[] array = buffer.ToArray();
            this.generator.MarkLabel(endIf);
            this.generator.Emit(OpCodes.Ldloca_S, arrayBuffer.LocalIndex);
            this.generator.EmitCall(
                OpCodes.Call,
                arrayBufferType.GetMethod(nameof(ArrayBuffer<int>.ToArray)),
                null);
        }

        private Label EmitCallReadBeginArray(Type elementType)
        {
            Label endIf = this.generator.DefineLabel();

            // if (this.ReadBeginArray(typeof(T)))
            this.generator.EmitLoadArgument(0);
            this.generator.EmitLoadTypeof(elementType);
            this.generator.EmitCall(this.baseClass, this.methods.ArraySerializer.ReadBeginArray);
            this.generator.Emit(OpCodes.Brfalse, endIf);
            return endIf;
        }

        private void EmitReadElement(Type bufferType, int bufferIndex, Type elementType)
        {
            // As ArrayBuffer is a value type, load it's address on the stack
            // so that we can call methods on it (i.e. the this argument)
            //
            // buffer.Add(...)
            this.generator.Emit(OpCodes.Ldloca_S, bufferIndex);

            Type underlyingType = Nullable.GetUnderlyingType(elementType);
            if (underlyingType != null)
            {
                this.EmitReadNullableElement(elementType, underlyingType);
            }
            else if (elementType.GetTypeInfo().IsValueType)
            {
                this.EmitReadValueElement(elementType);
            }
            else
            {
                this.EmitReadReferenceElement(elementType);
            }

            // buffer.Add(...)
            this.generator.EmitCall(
                OpCodes.Call,
                bufferType.GetMethod(nameof(ArrayBuffer<int>.Add)),
                null);
        }

        private void EmitReadElementSeparator(Label loopStart)
        {
            // if (this.ReadElementSeparator()) goto loopStart
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.ArraySerializer.ReadElementSeparator);
            this.generator.Emit(OpCodes.Brtrue, loopStart);
        }

        private void EmitReadNullableElement(Type nullableType, Type underlyingType)
        {
            Label elseIf = this.generator.DefineLabel();
            Label endIf = this.generator.DefineLabel();

            // if (this.Reader.ReadNull())
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.PrimitiveSerializer.GetReader);
            this.generator.EmitCall(typeof(ValueReader), this.methods.ValueReader.ReadNull);
            this.generator.Emit(OpCodes.Brfalse_S, elseIf);

            // default(Nullable<T>)
            int index = this.CreateLocal(nullableType).LocalIndex;
            this.generator.Emit(OpCodes.Ldloca_S, index);
            this.generator.Emit(OpCodes.Initobj, nullableType);
            this.generator.EmitLoadLocal(index);
            this.generator.Emit(OpCodes.Br_S, endIf);

            // else // i.e. ReadNull was false so we can read a value
            //    (T?)this.Reader.ReadXXX();
            this.generator.MarkLabel(elseIf);
            this.EmitReadValueElement(underlyingType);

            // new Nullable<T>(T)
            this.generator.Emit(OpCodes.Newobj, nullableType.GetConstructor(new[] { underlyingType }));
            this.generator.MarkLabel(endIf);
        }

        private void EmitReadReferenceElement(Type elementType)
        {
            Label elseIf = this.generator.DefineLabel();
            Label endIf = this.generator.DefineLabel();

            // if (this.Reader.ReadNull())
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.PrimitiveSerializer.GetReader);
            this.generator.EmitCall(typeof(ValueReader), this.methods.ValueReader.ReadNull);
            this.generator.Emit(OpCodes.Brfalse_S, elseIf);

            // null
            this.generator.Emit(OpCodes.Ldnull);
            this.generator.Emit(OpCodes.Br_S, endIf);

            // else
            //    this.Reader.ReadXXX();
            this.generator.MarkLabel(elseIf);
            this.ReadValue(this.generator, elementType);
            this.generator.MarkLabel(endIf);
        }

        private void EmitReadValueElement(Type elementType)
        {
            this.ReadValue(this.generator, elementType);
        }
    }
}
