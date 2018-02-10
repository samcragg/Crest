namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class ILGeneratorExtensionsTests
    {
        private const string GeneratedMethodName = "GeneratedMethod";
        private const string GeneratedTypeName = "GeneratedType";
        private readonly MethodBuilder methodBuilder;
        private readonly TypeBuilder typeBuilder;

        protected ILGeneratorExtensionsTests()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(ILGeneratorExtensionsTests) + "DynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            ModuleBuilder moduleBuilder =
                assemblyBuilder.DefineDynamicModule("Module");

            this.typeBuilder = moduleBuilder.DefineType(GeneratedTypeName);
            this.methodBuilder = this.typeBuilder.DefineMethod(GeneratedMethodName, MethodAttributes.Public | MethodAttributes.Static);
        }

        protected MethodInfo GetGeneratedMethod()
        {
            TypeInfo generated = this.typeBuilder.CreateTypeInfo();
            return generated.GetMethod(GeneratedMethodName);
        }

        public sealed class EmitCall : ILGeneratorExtensionsTests
        {
            private static string StaticResult;
            private string result;

            // Public so the generated code can access it
            public interface IExampleInterface
            {
                void Explicit();

                void Implicit();
            }

            [Fact]
            public void ShouldCallExplicitInterfaceMethods()
            {
                MethodInfo interfaceMethod = typeof(IExampleInterface).GetMethod(nameof(IExampleInterface.Explicit));

                // (new ExampleClass(this_passed_in)).Method()
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                this.EmitCreateExampleClass(generator);
                ILGeneratorExtensions.EmitCall(generator, typeof(ExampleClass), interfaceMethod);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                method.Invoke(null, new object[] { this });

                this.result.Should().Be(nameof(IExampleInterface.Explicit));
            }

            [Fact]
            public void ShouldCallInterfaceMethods()
            {
                MethodInfo interfaceMethod = typeof(IExampleInterface).GetMethod(nameof(IExampleInterface.Implicit));

                // (new ExampleClass(this_passed_in)).Method()
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                this.EmitCreateExampleClass(generator);
                ILGeneratorExtensions.EmitCall(generator, typeof(ExampleClass), interfaceMethod);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                method.Invoke(null, new object[] { this });

                this.result.Should().Be(nameof(IExampleInterface.Implicit));
            }

            [Fact]
            public void ShouldCallMethodsOnInterfaceTypes()
            {
                MethodInfo interfaceMethod = typeof(IExampleInterface).GetMethod(nameof(IExampleInterface.Implicit));

                // (new ExampleClass(this_passed_in)).Method()
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                this.EmitCreateExampleClass(generator);
                ILGeneratorExtensions.EmitCall(generator, typeof(IExampleInterface), interfaceMethod);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                method.Invoke(null, new object[] { this });

                this.result.Should().Be(nameof(IExampleInterface.Implicit));
            }

            [Fact]
            public void ShouldCallStaticMethods()
            {
                ILGenerator generator = this.methodBuilder.GetILGenerator();

                MethodInfo staticMethod = typeof(ExampleClass).GetMethod(nameof(ExampleClass.StaticMethod));

                ILGeneratorExtensions.EmitCall(generator, typeof(ExampleClass), staticMethod);
                generator.Emit(OpCodes.Ret);

                StaticResult = null;
                MethodInfo method = this.GetGeneratedMethod();
                method.Invoke(null, null);

                StaticResult.Should().Be(nameof(ExampleClass.StaticMethod));
            }

            private void EmitCreateExampleClass(ILGenerator generator)
            {
                this.methodBuilder.SetParameters(typeof(EmitCall));
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Newobj, typeof(ExampleClass).GetConstructor(new[] { typeof(EmitCall) }));
            }

            // Public so the generated code can access it
            public class ExampleClass : IExampleInterface
            {
                private readonly EmitCall parent;

                public ExampleClass(EmitCall parent)
                {
                    this.parent = parent;
                }

                public static void StaticMethod()
                {
                    StaticResult = nameof(StaticMethod);
                }

                public void Implicit()
                {
                    this.parent.result = nameof(Implicit);
                }

                void IExampleInterface.Explicit()
                {
                    this.parent.result = nameof(IExampleInterface.Explicit);
                }
            }
        }

        public sealed class EmitConvertToObject : ILGeneratorExtensionsTests
        {
            [Fact]
            public void ShouldConvertReferenceTypes()
            {
                this.EmitMethodReturningStringWithArgument<string>();

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, new object[] { "String Value" });

                result.Should().Be("String Value");
            }

            [Fact]
            public void ShouldConvertValueTypes()
            {
                this.EmitMethodReturningStringWithArgument<int>();

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, new object[] { 123 });

                result.Should().Be("123");
            }

            private void EmitMethodReturningStringWithArgument<T>()
            {
                // Creates a method like:
                //   string GeneratedMethod(X p) { return p.ToString(); }
                this.methodBuilder.SetParameters(typeof(T));
                this.methodBuilder.SetReturnType(typeof(string));
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);

                ILGeneratorExtensions.EmitConvertToObject(generator, typeof(T));

                generator.EmitCall(OpCodes.Callvirt, typeof(object).GetMethod(nameof(object.ToString)), null);
                generator.Emit(OpCodes.Ret);
            }
        }

        public sealed class EmitForLoop : ILGeneratorExtensionsTests
        {
            [Fact]
            public void ShouldCheckTheConditionAfterInitializingTheCounter()
            {
                this.methodBuilder.SetReturnType(typeof(int));
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                generator.DeclareLocal(typeof(int)); // Used for the counter

                // Because i starts at one, we're not expecting the body to be
                // executed at runtime, i.e.
                // for (int i = 1; i < 1; i++) { throw new InvalidOperationException(); }
                ILGeneratorExtensions.EmitForLoop(
                    generator,
                    0,
                    g => g.Emit(OpCodes.Ldc_I4_1), // Starts at 1
                    g => g.Emit(OpCodes.Ldc_I4_1), // Ends before 1
                    g =>
                    {
                        // throw new InvalidOperationException()
                        g.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes));
                        g.Emit(OpCodes.Throw);
                    });

                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                method.Invoking(m => m.Invoke(null, null))
                      .Should().NotThrow<InvalidOperationException>();
            }

            [Fact]
            public void ShouldRunTheLoopTheSpecifiedNumberOfTimes()
            {
                this.methodBuilder.SetReturnType(typeof(int));
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                generator.DeclareLocal(typeof(int)); // sum
                generator.DeclareLocal(typeof(int)); // i

                ILGeneratorExtensions.EmitForLoop(
                    generator,
                    1, // i
                    g => g.Emit(OpCodes.Ldc_I4_0),
                    g => g.Emit(OpCodes.Ldc_I4_3),
                    g =>
                    {
                        // sum = sum + 1;
                        g.Emit(OpCodes.Ldloc_0); // sum
                        g.Emit(OpCodes.Ldloc_1); // i
                        g.Emit(OpCodes.Add);
                        g.Emit(OpCodes.Stloc_0); // sum
                    });

                generator.Emit(OpCodes.Ldloc_0); // sum
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, null);

                result.Should().Be(0 + 1 + 2);
            }
        }

        public sealed class EmitLoadArgument : ILGeneratorExtensionsTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(4)]
            public void ShouldLoadTheSpecifiedArgument(int index)
            {
                // Create a method that has five arguments and simple returns
                // the argument at the specific index
                this.methodBuilder.SetParameters(typeof(int), typeof(int), typeof(int), typeof(int), typeof(int));
                this.methodBuilder.SetReturnType(typeof(int));
                ILGenerator generator = this.methodBuilder.GetILGenerator();

                ILGeneratorExtensions.EmitLoadArgument(generator, index);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, new object[] { 1, 2, 3, 4, 5 });

                result.Should().Be(index + 1);
            }
        }

        public sealed class EmitLoadElement : ILGeneratorExtensionsTests
        {
            [Theory]
            [InlineData(typeof(byte), default(byte))]
            [InlineData(typeof(char), default(char))]
            [InlineData(typeof(double), default(double))]
            [InlineData(typeof(float), default(float))]
            [InlineData(typeof(int), default(int))]
            [InlineData(typeof(long), default(long))]
            [InlineData(typeof(sbyte), default(sbyte))]
            [InlineData(typeof(short), default(short))]
            [InlineData(typeof(string), default(string))]
            [InlineData(typeof(uint), default(uint))]
            [InlineData(typeof(ulong), default(ulong))]
            [InlineData(typeof(ushort), default(ushort))]
            public void ShouldLoadElementsOfTheSpecifiedType(Type elementType, object defaultValue)
            {
                this.methodBuilder.SetReturnType(elementType);
                ILGenerator generator = this.methodBuilder.GetILGenerator();

                // new T[1]
                generator.Emit(OpCodes.Ldc_I4_1);
                generator.Emit(OpCodes.Newarr, elementType);

                // return array[0]
                generator.Emit(OpCodes.Ldc_I4_0);
                ILGeneratorExtensions.EmitLoadElement(generator, elementType);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, null);

                result.Should().Be(defaultValue);
            }

            [Fact]
            public void ShouldLoadValueTypes()
            {
                // We can't use attributes to define the default DateTime, as
                // it's not a constant value
                this.ShouldLoadElementsOfTheSpecifiedType(typeof(DateTime), default(DateTime));
            }
        }

        public sealed class EmitLoadLocal : ILGeneratorExtensionsTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(4)]
            public void ShouldLoadTheSpecifiedArgument(int index)
            {
                // Create a method that has five locals and simple returns
                // the value at the specific index
                this.methodBuilder.SetReturnType(typeof(int));
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                for (int i = 0; i < 5; i++)
                {
                    // int localX = i + 1
                    generator.DeclareLocal(typeof(int));
                    generator.Emit(OpCodes.Ldc_I4, i + 1);
                    generator.Emit(OpCodes.Stloc, i);
                }

                ILGeneratorExtensions.EmitLoadLocal(generator, index);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, null);

                result.Should().Be(index + 1);
            }
        }

        public sealed class EmitLoadTypeof : ILGeneratorExtensionsTests
        {
            [Fact]
            public void ShouldLoadTheSpecifiedType()
            {
                this.methodBuilder.SetReturnType(typeof(Type));
                ILGenerator generator = this.methodBuilder.GetILGenerator();

                ILGeneratorExtensions.EmitLoadTypeof(generator, typeof(int));
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, null);

                result.Should().Be(typeof(int));
            }
        }

        public sealed class EmitStoreLocal : ILGeneratorExtensionsTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(4)]
            public void ShouldStoreTheSpecifiedArgument(int index)
            {
                // Create a method that has five locals and simple returns
                // the value at the specific index after setting it to a value
                this.methodBuilder.SetReturnType(typeof(int));
                ILGenerator generator = this.methodBuilder.GetILGenerator();
                generator.DeclareLocal(typeof(int));
                generator.DeclareLocal(typeof(int));
                generator.DeclareLocal(typeof(int));
                generator.DeclareLocal(typeof(int));
                generator.DeclareLocal(typeof(int));
                generator.Emit(OpCodes.Ldc_I4_8);

                ILGeneratorExtensions.EmitStoreLocal(generator, index);

                generator.Emit(OpCodes.Ldloc_S, index);
                generator.Emit(OpCodes.Ret);

                MethodInfo method = this.GetGeneratedMethod();
                object result = method.Invoke(null, null);

                result.Should().Be(8);
            }
        }
    }
}
