namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ArraySerializeEmitterTests
    {
        private static ArraySerializeEmitter CreateArraySerializer<TBase>(ILGenerator generator)
        {
            return new ArraySerializeEmitter(
                generator,
                typeof(TBase),
                new Methods(typeof(TBase)));
        }

        // public so the dynamic code can inherit it
        public abstract class _ArraySerializerBase<T> : FakeSerializerBase
        {
            protected readonly List<T> values = new List<T>();

            protected _ArraySerializerBase()
            {
                this.Writer.When(x => x.WriteNull()).Do(_ => this.values.Add(default));
            }

            internal int BeginArraySize { get; private set; }

            internal Type BeginArrayType { get; private set; }

            internal int ElementSeparatorCount { get; private set; }

            internal int EndArrayCount { get; private set; }

            internal IReadOnlyList<T> Values => this.values;

            public override void WriteBeginArray(Type elementType, int size)
            {
                this.BeginArraySize = size;
                this.BeginArrayType = elementType;
            }

            public override void WriteElementSeparator()
            {
                this.ElementSeparatorCount++;
            }

            public override void WriteEndArray()
            {
                this.EndArrayCount++;
            }
        }

        public sealed class EmitWriteArray : ArraySerializeEmitterTests
        {
            private const string GeneratedMethodName = "Method";

            [Fact]
            public void ShouldCallBeginWriteArray()
            {
                _ArraySerializerBase<int> result = this.SerializeArray(new int[12]);

                result.BeginArraySize.Should().Be(12);
                result.BeginArrayType.Should().Be(typeof(int));
            }

            [Fact]
            public void ShouldCallWriteElementSeparatorBetweenElements()
            {
                _ArraySerializerBase<int> result = this.SerializeArray(new int[3]);

                // 1 separator 2 separator 3
                result.ElementSeparatorCount.Should().Be(2);
            }

            [Fact]
            public void ShouldCallWriteEndArray()
            {
                _ArraySerializerBase<int> result = this.SerializeArray(new int[0]);

                result.EndArrayCount.Should().Be(1);
            }

            [Fact]
            public void ShouldHandleBaseArrayClass()
            {
                // Create the dynamic assembly related types
                TypeBuilder typeBuilder = CreateTypeBuilder<_ArraySerializerBase<int>>();
                MethodBuilder method = CreateMethod(typeBuilder, typeof(Array));

                // Create the method
                ILGenerator generator = method.GetILGenerator();
                generator.DeclareLocal(typeof(int)); // loop counter
                ArraySerializeEmitter emitter = CreateArraySerializer<_ArraySerializerBase<int>>(generator);
                emitter.LoadArray = g => g.EmitLoadArgument(1); // 0 = this, 1 = Array
                emitter.LoadArrayElement = (g, _) => g.EmitCall(OpCodes.Callvirt, typeof(IList).GetProperty("Item").GetGetMethod(), null);
                emitter.LoadArrayLength = g => g.EmitCall(OpCodes.Callvirt, typeof(Array).GetProperty(nameof(Array.Length)).GetGetMethod(), null);
                emitter.LoopCounterLocalIndex = 0;
                emitter.WriteValue = (_, loadElement) =>
                {
                    // this.values.Add(x);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, typeof(_ArraySerializerBase<int>).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic));
                    loadElement(generator);
                    generator.Emit(OpCodes.Unbox_Any, typeof(int));
                    generator.EmitCall(OpCodes.Callvirt, typeof(List<int>).GetMethod(nameof(List<int>.Add)), null);
                };

                emitter.EmitWriteArray(typeof(int[]));
                generator.Emit(OpCodes.Ret);

                _ArraySerializerBase<int> result = InvokeGeneratedMethod<int>(
                    typeBuilder,
                    new[] { 1, 2, 3 });

                result.Values.Should().Equal(1, 2, 3);
            }

            [Fact]
            public void ShouldNotCallWriteElementSeparatorForSingleElementArrayss()
            {
                _ArraySerializerBase<int> result = this.SerializeArray(new int[1]);

                result.ElementSeparatorCount.Should().Be(0);
            }

            [Fact]
            public void ShouldSerializeNullableElements()
            {
                _ArraySerializerBase<int?> result = this.SerializeArray(
                    new int?[] { null, 123 });

                result.Values.Should().Equal(null, 123);
            }

            [Fact]
            public void ShouldSerializerReferenceElements()
            {
                _ArraySerializerBase<string> result = this.SerializeArray(
                    new string[] { null, "string" });

                result.Values.Should().Equal(null, "string");
            }

            [Fact]
            public void ShouldSerializerValueTypeElements()
            {
                _ArraySerializerBase<int> result = this.SerializeArray(
                    new int[] { 1, 2 });

                result.Values.Should().Equal(1, 2);
            }

            private static MethodBuilder CreateMethod(TypeBuilder typeBuilder, Type parameter)
            {
                MethodBuilder method = typeBuilder.DefineMethod(
                    GeneratedMethodName,
                    MethodAttributes.HideBySig | MethodAttributes.Public,
                    CallingConventions.HasThis);
                method.SetParameters(parameter);
                return method;
            }

            private static TypeBuilder CreateTypeBuilder<TBase>()
            {
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                                    new AssemblyName(nameof(ArraySerializeEmitterTests) + "_DynamicAssembly"),
                                    AssemblyBuilderAccess.RunAndCollect);

                TypeBuilder typeBuilder =
                    assemblyBuilder.DefineDynamicModule("Module").DefineType(
                        "TestType",
                        TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                        TypeAttributes.BeforeFieldInit | TypeAttributes.Public,
                        typeof(TBase));

                typeBuilder.DefineDefaultConstructor(
                    MethodAttributes.HideBySig |
                    MethodAttributes.Public |
                    MethodAttributes.RTSpecialName |
                    MethodAttributes.SpecialName);

                return typeBuilder;
            }

            private static _ArraySerializerBase<T> InvokeGeneratedMethod<T>(TypeBuilder typeBuilder, object parameter)
            {
                TypeInfo typeInfo = typeBuilder.CreateTypeInfo();
                object instance = Activator.CreateInstance(typeInfo.AsType());

                typeInfo.GetMethod(GeneratedMethodName)
                        .Invoke(instance, new object[] { parameter });

                return (_ArraySerializerBase<T>)instance;
            }

            private _ArraySerializerBase<T> SerializeArray<T>(T[] array = null)
            {
                // Create the dynamic assembly related types
                TypeBuilder typeBuilder = CreateTypeBuilder<_ArraySerializerBase<T>>();
                MethodBuilder method = CreateMethod(typeBuilder, typeof(T[]));

                // Create the method
                ILGenerator generator = method.GetILGenerator();
                generator.DeclareLocal(typeof(int)); // loop counter
                generator.DeclareLocal(typeof(T[])); // array variable
                ArraySerializeEmitter emitter = CreateArraySerializer<_ArraySerializerBase<T>>(generator);
                emitter.LoadArray = g => g.EmitLoadLocal(1);
                emitter.LoopCounterLocalIndex = 0;
                emitter.WriteValue = (_, loadElement) =>
                {
                    // this.values.Add(x);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(
                        OpCodes.Ldfld,
                        typeof(_ArraySerializerBase<T>).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic));

                    loadElement(generator);

                    // If it's a nullable value then we would have been passed
                    // the actual value, so convert it back to a nullable one
                    // to store in the list.
                    if (Nullable.GetUnderlyingType(typeof(T)) != null)
                    {
                        generator.Emit(
                            OpCodes.Newobj,
                            typeof(T).GetConstructor(new[] { Nullable.GetUnderlyingType(typeof(T)) }));
                    }

                    generator.EmitCall(OpCodes.Callvirt, typeof(List<T>).GetMethod(nameof(List<T>.Add)), null);
                };

                // Generate the code
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Stloc_1);
                emitter.EmitWriteArray(typeof(T[]));
                generator.Emit(OpCodes.Ret);

                return InvokeGeneratedMethod<T>(typeBuilder, array ?? new T[0]);
            }
        }
    }
}
