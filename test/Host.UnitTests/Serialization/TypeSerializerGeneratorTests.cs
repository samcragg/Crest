﻿namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
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

                MethodBuilder methodBuilder = CreateMethod(typeBuilder);
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

            void IPrimitiveSerializer<string>.BeginWrite(string metadata)
            {
            }

            void IPrimitiveSerializer<string>.EndWrite()
            {
            }

            void IPrimitiveSerializer<string>.Flush()
            {
            }

            bool IArraySerializer.ReadBeginArray(Type elementType)
            {
                return false;
            }

            void IClassSerializer<string>.ReadBeginClass(string metadata)
            {
            }

            string IClassSerializer<string>.ReadBeginProperty()
            {
                return null;
            }

            bool IArraySerializer.ReadElementSeparator()
            {
                return false;
            }

            void IArraySerializer.ReadEndArray()
            {
            }

            void IClassSerializer<string>.ReadEndClass()
            {
            }

            void IClassSerializer<string>.ReadEndProperty()
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
                EmitCallBeginMethodWithTypeMetadata(
                    new TypeSerializerBuilder(this, builder, typeof(int)),
                    generator,
                    method);
            }

            public void EmitConstructor(Type parameter)
            {
                EmitConstructor(null, null, parameter);
            }
        }
    }
}
