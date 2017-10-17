// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Contains extension methods for the <see cref="ILGenerator"/> class.
    /// </summary>
    internal static class ILGeneratorExtensions
    {
        /// <summary>
        /// Calls the specified method.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="target">The type to invoke the method against.</param>
        /// <param name="method">The method to invoke.</param>
        public static void EmitCall(this ILGenerator generator, Type target, MethodInfo method)
        {
            // Class methods implementing an interface method are sealed by
            // default, so we can use the normal call to invoke them. For
            // explicit methods, the C# compiler emits a call virtual to the
            // private method that explicitly implements the interface method.
            // If we try to do the same then we get a TypeAccessException :*(
            // Therefore, we'll get the interface method passed in and try to
            // find it on the map to see if we can call the public method
            // directly or if we invoke it virtually via the interface (or it
            // could just be a normal method)
            if (method.DeclaringType.GetTypeInfo().IsInterface && (target != method.DeclaringType))
            {
                InterfaceMapping map = target.GetTypeInfo().GetRuntimeInterfaceMap(method.DeclaringType);
                int index = Array.IndexOf(map.InterfaceMethods, method);

                // We can only invoke public methods in our emitted code - the
                // method will be private for explicit methods which we need to
                // invoke via the interface
                if (map.TargetMethods[index].IsPublic)
                {
                    method = map.TargetMethods[index];
                }
            }

            if (method.IsStatic || method.IsFinal)
            {
                generator.EmitCall(OpCodes.Call, method, null);
            }
            else
            {
                generator.EmitCall(OpCodes.Callvirt, method, null);
            }
        }

        /// <summary>
        /// Ensures the value on top of the evaluation stack is an object,
        /// boxing as required.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="type">The type on the stack.</param>
        public static void EmitConvertToObject(this ILGenerator generator, Type type)
        {
            if (type.GetTypeInfo().IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        /// <summary>
        /// Emits a for loop consisting of an integer starting at a specific
        /// value and ending when it's reached a specific value.
        /// </summary>
        /// <param name="generator">The generator to emit the instructions to.</param>
        /// <param name="counterIndex">The local index of the counter variable.</param>
        /// <param name="loadStartValue">Loads the starting value.</param>
        /// <param name="loadEndValue">Loads the ending value.</param>
        /// <param name="body">The main for loop body.</param>
        public static void EmitForLoop(
            this ILGenerator generator,
            int counterIndex,
            Action<ILGenerator> loadStartValue,
            Action<ILGenerator> loadEndValue,
            Action<ILGenerator> body)
        {
            Label forLoopBody = generator.DefineLabel();
            Label forLoopCheck = generator.DefineLabel();

            // for (int i = startValue; ...; ...)
            loadStartValue(generator);
            generator.EmitStoreLocal(counterIndex);
            generator.Emit(OpCodes.Br_S, forLoopCheck);

            generator.MarkLabel(forLoopBody);
            body(generator);

            // for (...; ...; i++)
            generator.EmitLoadLocal(counterIndex);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.EmitStoreLocal(counterIndex);

            // for (...; i < endValue; ...)
            generator.MarkLabel(forLoopCheck);
            generator.EmitLoadLocal(counterIndex);
            loadEndValue(generator);
            generator.Emit(OpCodes.Blt_S, forLoopBody);
        }

        /// <summary>
        /// Loads the argument at the specific index onto the evaluation stack.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="position">The index of the argument to load.</param>
        public static void EmitLoadArgument(this ILGenerator generator, int position)
        {
            switch (position)
            {
                case 0:
                    generator.Emit(OpCodes.Ldarg_0);
                    break;

                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    break;

                case 2:
                    generator.Emit(OpCodes.Ldarg_2);
                    break;

                case 3:
                    generator.Emit(OpCodes.Ldarg_3);
                    break;

                default:
                    generator.Emit(OpCodes.Ldarg_S, (byte)position);
                    break;
            }
        }

        /// <summary>
        /// Loads the element of the array onto the evaluation stack.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="type">The element type of the array.</param>
        public static void EmitLoadElement(this ILGenerator generator, Type type)
        {
            if (!type.GetTypeInfo().IsValueType)
            {
                generator.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                EmitLoadValueElement(generator, type);
            }
        }

        /// <summary>
        /// Loads the local variable at the specific index onto the evaluation stack.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="position">The index of the local to load from.</param>
        public static void EmitLoadLocal(this ILGenerator generator, int position)
        {
            switch (position)
            {
                case 0:
                    generator.Emit(OpCodes.Ldloc_0);
                    break;

                case 1:
                    generator.Emit(OpCodes.Ldloc_1);
                    break;

                case 2:
                    generator.Emit(OpCodes.Ldloc_2);
                    break;

                case 3:
                    generator.Emit(OpCodes.Ldloc_3);
                    break;

                default:
                    generator.Emit(OpCodes.Ldloc_S, (byte)position);
                    break;
            }
        }

        /// <summary>
        /// Loads the specified type onto the evaluation stack.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="type">The type to load.</param>
        public static void EmitLoadTypeof(this ILGenerator generator, Type type)
        {
            generator.Emit(OpCodes.Ldtoken, type);
            generator.Emit(
                OpCodes.Call,
                typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
        }

        /// <summary>
        /// Pops the current value from the top of the evaluation stack and
        /// stores it in a the local variable list at index.
        /// </summary>
        /// <param name="generator">The generator to emit the instruction to.</param>
        /// <param name="position">The index of the local to store to.</param>
        public static void EmitStoreLocal(this ILGenerator generator, int position)
        {
            switch (position)
            {
                case 0:
                    generator.Emit(OpCodes.Stloc_0);
                    break;

                case 1:
                    generator.Emit(OpCodes.Stloc_1);
                    break;

                case 2:
                    generator.Emit(OpCodes.Stloc_2);
                    break;

                case 3:
                    generator.Emit(OpCodes.Stloc_3);
                    break;

                default:
                    generator.Emit(OpCodes.Stloc_S, (byte)position);
                    break;
            }
        }

        private static void EmitLoadValueElement(ILGenerator generator, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    generator.Emit(OpCodes.Ldelem_I1);
                    break;

                case TypeCode.Byte:
                    generator.Emit(OpCodes.Ldelem_U1);
                    break;

                case TypeCode.Int16:
                    generator.Emit(OpCodes.Ldelem_I2);
                    break;

                case TypeCode.Char:
                case TypeCode.UInt16:
                    generator.Emit(OpCodes.Ldelem_U2);
                    break;

                case TypeCode.Int32:
                    generator.Emit(OpCodes.Ldelem_I4);
                    break;

                case TypeCode.UInt32:
                    generator.Emit(OpCodes.Ldelem_U4);
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    generator.Emit(OpCodes.Ldelem_I8);
                    break;

                case TypeCode.Single:
                    generator.Emit(OpCodes.Ldelem_R4);
                    break;

                case TypeCode.Double:
                    generator.Emit(OpCodes.Ldelem_R8);
                    break;

                default:
                    generator.Emit(OpCodes.Ldelem, type);
                    break;
            }
        }
    }
}
