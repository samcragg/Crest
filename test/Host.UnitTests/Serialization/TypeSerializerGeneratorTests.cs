namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class TypeSerializerGeneratorTests
    {
        public sealed class EmitCallBeginMethodWithTypeMetadata : TypeSerializerGeneratorTests
        {
            private const string GeneratedMethodName = "GeneratedMethod";

            [Fact]
            public void ShouldLoadNullIfThereIsNoGetTypeMetadataMethod()
            {
                TypeBuilder typeBuilder =
                    EmitHelper.CreateTypeBuilder<ClassWithNoGetTypeMetadata>();

                MethodBuilder methodBuilder = this.CreateMethod(typeBuilder);
                ILGenerator ilGenerator = methodBuilder.GetILGenerator();

                var generator = new FakeTypeSerializeGenerator(
                    (ModuleBuilder)typeBuilder.Module,
                    typeof(ClassWithNoGetTypeMetadata));

                generator.EmitCallBeginMethodWithTypeMetadata(
                    typeBuilder,
                    ilGenerator,
                    typeof(ClassWithNoGetTypeMetadata).GetMethod(nameof(ClassWithNoGetTypeMetadata.CallWithMetadata)));
                ilGenerator.Emit(OpCodes.Ret);

                ClassWithNoGetTypeMetadata instance = InvokeGeneratedMethod<ClassWithNoGetTypeMetadata>(typeBuilder);

                instance.Metadata.Should().BeNull();
            }

            private static T InvokeGeneratedMethod<T>(TypeBuilder typeBuilder)
            {
                TypeInfo typeInfo = typeBuilder.CreateTypeInfo();
                object instance = Activator.CreateInstance(typeInfo.AsType());

                typeInfo.GetMethod(GeneratedMethodName)
                        .Invoke(instance, null);

                return (T)instance;
            }

            private MethodBuilder CreateMethod(TypeBuilder typeBuilder)
            {
                MethodBuilder method = typeBuilder.DefineMethod(
                    GeneratedMethodName,
                    MethodAttributes.HideBySig | MethodAttributes.Public,
                    CallingConventions.HasThis);
                return method;
            }

            public class ClassWithNoGetTypeMetadata : PrimitiveSerializer
            {
                internal string Metadata { get; private set; }

                public void CallWithMetadata(string metadata)
                {
                    this.Metadata = metadata;
                }
            }
        }

        public sealed class EmitConstructor : TypeSerializerGeneratorTests
        {
            [Fact]
            public void ShouldThrowIfTheConstructorDoesNotExist()
            {
                var generator = new FakeTypeSerializeGenerator(
                    null,
                    typeof(ClassWithProtectedConstructors));

                generator.Invoking(g => g.EmitConstructor(typeof(string)))
                         .Should().Throw<InvalidOperationException>()
                         .WithMessage("*String*"); // It should output the type of the parameter
            }

            [Fact]
            public void ShouldThrowIfTheConstructorIsNotAccessible()
            {
                var generator = new FakeTypeSerializeGenerator(
                    null,
                    typeof(ClassWithPrivateConstructor));

                generator.Invoking(g => g.EmitConstructor(typeof(int)))
                         .Should().Throw<InvalidOperationException>();
            }

            private class ClassWithPrivateConstructor : PrimitiveSerializer
            {
                private ClassWithPrivateConstructor(int value)
                {
                }
            }

            private class ClassWithProtectedConstructors : PrimitiveSerializer
            {
                protected ClassWithProtectedConstructors(int first, string second)
                {
                }

                protected ClassWithProtectedConstructors(int value)
                {
                }
            }
        }

        public abstract class PrimitiveSerializer : IClassSerializer<string>
        {
            public ValueReader Reader => null;

            public ValueWriter Writer => null;

            public static string GetMetadata()
            {
                return null;
            }

            public void BeginRead(string metadata)
            {
            }

            public void EndRead()
            {
            }

            public void ReadBeginClass(string metadata)
            {
            }

            public void WriteBeginClass(string metadata)
            {
            }

            public void WriteBeginProperty(string propertyMetadata)
            {
            }

            void IPrimitiveSerializer<string>.BeginWrite(string metadata)
            {
            }

            void IPrimitiveSerializer<string>.EndWrite()
            {
            }

            bool IClassReader.ReadBeginArray(Type elementType)
            {
                return false;
            }

            string IClassReader.ReadBeginProperty()
            {
                return null;
            }

            bool IClassReader.ReadElementSeparator()
            {
                return false;
            }

            void IClassReader.ReadEndArray()
            {
            }

            void IClassReader.ReadEndClass()
            {
            }

            void IClassReader.ReadEndProperty()
            {
            }

            void IClassWriter.WriteBeginArray(Type elementType, int size)
            {
            }

            void IClassWriter.WriteElementSeparator()
            {
            }

            void IClassWriter.WriteEndArray()
            {
            }

            void IClassWriter.WriteEndClass()
            {
            }

            void IClassWriter.WriteEndProperty()
            {
            }
        }

        private sealed class FakeTypeSerializeGenerator : TypeSerializerGenerator
        {
            public FakeTypeSerializeGenerator(ModuleBuilder module, Type baseClass)
                : base(module, baseClass)
            {
            }

            public void EmitCallBeginMethodWithTypeMetadata(
                TypeBuilder builder,
                ILGenerator generator,
                MethodInfo method)
            {
                this.EmitCallBeginMethodWithTypeMetadata(
                    new TypeSerializerBuilder(this, builder, typeof(int), null, null),
                    generator,
                    method);
            }

            public void EmitConstructor(Type parameter)
            {
                this.EmitConstructor(null, null, parameter);
            }
        }
    }
}
