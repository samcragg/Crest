namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class TypeSerializerGeneratorTests
    {
        public sealed class EmitConstructor : TypeSerializerGeneratorTests
        {
            [Fact]
            public void ShouldThrowIfTheConstructorDoesNotExist()
            {
                var generator = new FakeTypeSerializeGenerator(
                    null,
                    typeof(ClassWithProtectedConstructors));

                generator.Invoking(g => g.EmitConstructor(null, typeof(string)))
                         .ShouldThrow<InvalidOperationException>()
                         .WithMessage("*String*"); // It should output the type of the parameter
            }

            [Fact]
            public void ShouldThrowIfTheConstructorIsNotAccessible()
            {
                var generator = new FakeTypeSerializeGenerator(
                    null,
                    typeof(ClassWithPrivateConstructor));

                generator.Invoking(g => g.EmitConstructor(null, typeof(int)))
                         .ShouldThrow<InvalidOperationException>();
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

            private abstract class PrimitiveSerializer : IClassSerializer<string>
            {
                public IStreamWriter Writer => null;

                public static string GetMetadata()
                {
                    return null;
                }

                void IPrimitiveSerializer<string>.BeginWrite(string metadata)
                {
                }

                void IPrimitiveSerializer<string>.EndWrite()
                {
                }

                void IPrimitiveSerializer<string>.Flush()
                {
                }

                void IArraySerializer.WriteBeginArray(Type elementType, int size)
                {
                }

                void IClassSerializer<string>.WriteBeginClass(string metadata)
                {
                }

                void IClassSerializer<string>.WriteBeginProperty(string propertyMetadata)
                {
                }

                void IArraySerializer.WriteElementSeparator()
                {
                }

                void IArraySerializer.WriteEndArray()
                {
                }

                void IClassSerializer<string>.WriteEndClass()
                {
                }

                void IClassSerializer<string>.WriteEndProperty()
                {
                }
            }
        }

        private sealed class FakeTypeSerializeGenerator : TypeSerializerGenerator
        {
            public FakeTypeSerializeGenerator(ModuleBuilder module, Type baseClass)
                : base(module, baseClass)
            {
            }

            public void EmitConstructor(TypeBuilder builder, Type parameter)
            {
                base.EmitConstructor(builder, null, parameter);
            }
        }
    }
}
