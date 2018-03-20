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
    /// Emits the code for serializing an array of items.
    /// </summary>
    internal sealed class ArraySerializeEmitter
    {
        // This class generates the following code:
        //
        //     this.WriteBeginArray(typeof(T), array.Length);
        //     if (array.Length > 0)
        //     {
        //         if (array[0] != null)
        //         {
        //             this.Write(array[0]);
        //         }
        //         else
        //         {
        //             this.WriteNull();
        //         }
        //
        //         for (int i = 1; i < array.Length; i++)
        //         {
        //             this.WriteElementSeparator();
        //             if (array[i] != null)
        //             {
        //                 this.Write(array[i]);
        //             }
        //             else
        //             {
        //                 this.WriteNull();
        //             }
        //         }
        //     }
        //     this.WriteEndArray(typeof(T));
        private readonly Type baseClass;
        private readonly ILGenerator generator;
        private readonly Methods methods;
        private int arrayLocalIndex;
        private int loopCounterLocalIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySerializeEmitter"/> class.
        /// </summary>
        /// <param name="generator">Where to emit the code to.</param>
        /// <param name="baseClass">The type of the base serializer class.</param>
        /// <param name="methods">Contains method metadata.</param>
        public ArraySerializeEmitter(ILGenerator generator, Type baseClass, Methods methods)
        {
            this.baseClass = baseClass;
            this.generator = generator;
            this.methods = methods;
        }

        /// <summary>
        /// Gets or sets the action to call to write the value to the output
        /// stream.
        /// </summary>
        /// <remarks>
        /// The first argument is the type of the value being written. The
        /// second argument is used to load the value onto the evaluation stack.
        /// </remarks>
        public Action<Type, Action<ILGenerator>> WriteValue { get; set; }

        /// <summary>
        /// Emits the code to serialize all the elements of an array.
        /// </summary>
        /// <param name="arrayType">The type of the array.</param>
        /// <remarks>
        /// This method assumes that the array has been loaded into the
        /// evaluation stack.
        /// </remarks>
        public void EmitWriteArray(Type arrayType)
        {
            Type elementType = arrayType.GetElementType();
            this.loopCounterLocalIndex = this.generator.DeclareLocal(typeof(int)).LocalIndex;
            this.arrayLocalIndex = this.generator.DeclareLocal(arrayType).LocalIndex;

            // var array = (T[])parameter
            this.generator.Emit(OpCodes.Castclass, arrayType);
            this.generator.EmitStoreLocal(this.arrayLocalIndex);

            // this.WriteBeginArray(elementType)
            this.CallWriteBeginArray(elementType);
            Label endIf = this.EmitLengthCheck();

            this.EmitWriteElement(elementType, g => g.Emit(OpCodes.Ldc_I4_0));
            this.EmitForLoop(elementType);

            // this.WriteEndArray();
            this.generator.MarkLabel(endIf);
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.ArraySerializer.WriteEndArray);
        }

        private void CallWriteBeginArray(Type elementType)
        {
            // this.WriteBeginArray(typeof(T), array.Length);
            this.generator.EmitLoadArgument(0);
            this.generator.EmitLoadTypeof(elementType);
            this.generator.EmitLoadLocal(this.arrayLocalIndex);
            this.generator.Emit(OpCodes.Ldlen); // Loads a natural unsigned int
            this.generator.Emit(OpCodes.Conv_I4);
            this.generator.EmitCall(this.baseClass, this.methods.ArraySerializer.WriteBeginArray);
        }

        private void EmitForLoop(Type elementType)
        {
            // for (int i = 1; i < array.Length; i++)
            this.generator.EmitForLoop(
                this.loopCounterLocalIndex,
                g => g.Emit(OpCodes.Ldc_I4_1),
                g =>
                {
                    // i < array.Length
                    this.generator.EmitLoadLocal(this.arrayLocalIndex);
                    g.Emit(OpCodes.Ldlen);
                    g.Emit(OpCodes.Conv_I4);
                },
                g =>
                {
                    // this.WriteElementSeparator();
                    g.EmitLoadArgument(0);
                    g.EmitCall(this.baseClass, this.methods.ArraySerializer.WriteElementSeparator);

                    // this.Writer.WriteXXX(local[i]);
                    this.EmitWriteElement(elementType, gen => gen.EmitLoadLocal(this.loopCounterLocalIndex));
                });
        }

        private Label EmitLengthCheck()
        {
            Label endIf = this.generator.DefineLabel();
            this.generator.EmitLoadLocal(this.arrayLocalIndex);
            this.generator.Emit(OpCodes.Ldlen);
            this.generator.Emit(OpCodes.Brfalse, endIf);
            return endIf;
        }

        private void EmitWriteElement(Type elementType, Action<ILGenerator> loadIndex)
        {
            Type underlyingType = Nullable.GetUnderlyingType(elementType);
            if (underlyingType != null)
            {
                this.EmitWriteNullableElement(elementType, underlyingType, loadIndex);
            }
            else if (elementType.GetTypeInfo().IsValueType)
            {
                this.EmitWriteElementValue(elementType, loadIndex);
            }
            else
            {
                this.EmitWriteReferenceElement(elementType, loadIndex);
            }
        }

        private void EmitWriteElementValue(Type elementType, Action<ILGenerator> loadIndex)
        {
            this.WriteValue(elementType, g =>
            {
                // array[index]
                g.EmitLoadLocal(this.arrayLocalIndex);
                loadIndex(g);
                g.EmitLoadElement(elementType);
            });
        }

        private void EmitWriteNull()
        {
            // this.Writer.WriteNull()
            this.generator.EmitLoadArgument(0);
            this.generator.EmitCall(this.baseClass, this.methods.PrimitiveSerializer.GetWriter);
            this.generator.EmitCall(typeof(ValueWriter), this.methods.ValueWriter.WriteNull);
        }

        private void EmitWriteNullableElement(Type elementType, Type underlyingType, Action<ILGenerator> loadIndex)
        {
            Label elseLabel = this.generator.DefineLabel();
            Label endIfLabel = this.generator.DefineLabel();

            MethodInfo getValueOrDefault = elementType.GetMethod(
                nameof(Nullable<int>.GetValueOrDefault),
                Type.EmptyTypes);

            MethodInfo hasValue =
                elementType.GetProperty(nameof(Nullable<int>.HasValue))
                           .GetGetMethod();

            // We need to load the address on the stack (ldelema) so that we
            // can call methods on it, as Nullable is a value type so the 'this'
            // pointer is the address of the memory location
            //
            // if (array[index].HasValue)
            this.generator.EmitLoadLocal(this.arrayLocalIndex);
            loadIndex(this.generator);
            this.generator.Emit(OpCodes.Ldelema, elementType);
            this.generator.EmitCall(OpCodes.Call, hasValue, null);
            this.generator.Emit(OpCodes.Brfalse_S, elseLabel);

            this.WriteValue(underlyingType, g =>
            {
                // array[index].GetValueOrDefault()
                g.EmitLoadLocal(this.arrayLocalIndex);
                loadIndex(g);
                g.Emit(OpCodes.Ldelema, elementType);
                this.generator.EmitCall(OpCodes.Call, getValueOrDefault, null);
            });
            this.generator.Emit(OpCodes.Br_S, endIfLabel);

            // else // i.e. array[index] == null
            this.generator.MarkLabel(elseLabel);
            this.EmitWriteNull();
            this.generator.MarkLabel(endIfLabel);
        }

        private void EmitWriteReferenceElement(Type elementType, Action<ILGenerator> loadIndex)
        {
            Label elseLabel = this.generator.DefineLabel();
            Label endIfLabel = this.generator.DefineLabel();

            // if (array[index] != null) { Write(array[index]) }
            this.generator.EmitLoadLocal(this.arrayLocalIndex);
            loadIndex(this.generator);
            this.generator.EmitLoadElement(elementType);
            this.generator.Emit(OpCodes.Brfalse_S, elseLabel);
            this.EmitWriteElementValue(elementType, loadIndex);
            this.generator.Emit(OpCodes.Br_S, endIfLabel);

            // else // i.e. array[index] == null
            this.generator.MarkLabel(elseLabel);
            this.EmitWriteNull();
            this.generator.MarkLabel(endIfLabel);
        }
    }
}
