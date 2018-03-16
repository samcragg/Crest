namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ArrayDeserializeEmitterTests
    {
        // public so the dynamic code can inherit it
        public abstract class _ArraySerializerBase : FakeSerializerBase
        {
            private int index;

            internal int EndArrayCount { get; private set; }

            internal int TotalValues { get; set; }

            public override bool ReadBeginArray(Type elementType)
            {
                return this.TotalValues > 0;
            }

            public override bool ReadElementSeparator()
            {
                // Increase first as ReadElementSeparator is called after a
                // value is read
                this.index++;
                return this.index < this.TotalValues;
            }

            public override void ReadEndArray()
            {
                this.EndArrayCount++;
            }
        }

        public sealed class EmitReadArray : ArrayDeserializeEmitterTests
        {
            private const string GeneratedMethodName = "Method";

            [Fact]
            public void ShouldCallReadEndArray()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<int>(nameof(ValueReader.ReadInt32));
                deserializer.TotalValues = 1;

                InvokeGeneratedMethod<int>(deserializer);

                deserializer.EndArrayCount.Should().Be(1);
            }

            [Fact]
            public void ShouldDeserializeEmptyArrays()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<int>(nameof(ValueReader.ReadInt32));

                int[] result = InvokeGeneratedMethod<int>(deserializer);

                result.Should().BeEmpty();
                deserializer.EndArrayCount.Should().Be(0);
            }

            [Fact]
            public void ShouldDeserializeMultipleItems()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<int>(nameof(ValueReader.ReadInt32));
                deserializer.TotalValues = 3;
                deserializer.Reader.ReadInt32().Returns(1, 2, 3);

                int[] result = InvokeGeneratedMethod<int>(deserializer);

                result.Should().Equal(1, 2, 3);
            }

            [Fact]
            public void ShouldDeserializeNullableElements()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<int?>(nameof(ValueReader.ReadInt32));
                deserializer.TotalValues = 2;
                deserializer.Reader.ReadNull().Returns(true, false);
                deserializer.Reader.ReadInt32().Returns(123);

                int?[] result = InvokeGeneratedMethod<int?>(deserializer);

                result.Should().Equal(null, 123);
            }

            [Fact]
            public void ShouldDeserializerReferenceElements()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<string>(nameof(ValueReader.ReadString));
                deserializer.TotalValues = 2;
                deserializer.Reader.ReadNull().Returns(true, false);
                deserializer.Reader.ReadString().Returns("string");

                string[] result = InvokeGeneratedMethod<string>(deserializer);

                result.Should().Equal(null, "string");
            }

            [Fact]
            public void ShouldDeserializerValueTypeElements()
            {
                _ArraySerializerBase deserializer = this.GenerateDeserializer<long>(nameof(ValueReader.ReadInt64));
                deserializer.TotalValues = 1;
                deserializer.Reader.ReadInt64().Returns(123);

                long[] result = InvokeGeneratedMethod<long>(deserializer);

                result.Should().Equal(123);
            }

            private static MethodBuilder CreateMethod(TypeBuilder typeBuilder, Type parameter = null, Type returnType = null)
            {
                MethodBuilder method = typeBuilder.DefineMethod(
                    GeneratedMethodName,
                    MethodAttributes.HideBySig | MethodAttributes.Public,
                    CallingConventions.HasThis);

                if (parameter != null)
                {
                    method.SetParameters(parameter);
                }

                if (returnType != null)
                {
                    method.SetReturnType(returnType);
                }

                return method;
            }

            private static TypeBuilder CreateTypeBuilder<TBase>()
            {
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                                    new AssemblyName(nameof(ArrayDeserializeEmitterTests) + "_DynamicAssembly"),
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

            private static T[] InvokeGeneratedMethod<T>(_ArraySerializerBase instance)
            {
                return (T[])instance.GetType()
                                    .GetMethod(GeneratedMethodName)
                                    .Invoke(instance, null);
            }

            private _ArraySerializerBase GenerateDeserializer<T>(string readMethod)
            {
                // Create the dynamic assembly related types
                TypeBuilder typeBuilder = CreateTypeBuilder<_ArraySerializerBase>();
                MethodBuilder method = CreateMethod(typeBuilder, returnType: typeof(T[]));

                // Create the method
                ILGenerator generator = method.GetILGenerator();
                Type baseClass = typeof(_ArraySerializerBase);
                var emitter = new ArrayDeserializeEmitter(generator, baseClass, new Methods(baseClass))
                {
                    CreateLocal = generator.DeclareLocal,
                    ReadValue = (g, _) =>
                    {
                        // this.Reader.ReadMethod()
                        g.Emit(OpCodes.Ldarg_0);

                        g.EmitCall(
                            OpCodes.Callvirt,
                            typeof(_ArraySerializerBase).GetProperty(nameof(_ArraySerializerBase.Reader)).GetGetMethod(),
                            null);

                        g.EmitCall(
                            OpCodes.Callvirt,
                            typeof(ValueReader).GetMethod(readMethod),
                            null);
                    }
                };

                // Generate the code
                emitter.EmitReadArray(typeof(T[]));
                generator.Emit(OpCodes.Ret);

                return (_ArraySerializerBase)Activator.CreateInstance(typeBuilder.CreateTypeInfo().AsType());
            }
        }
    }
}
