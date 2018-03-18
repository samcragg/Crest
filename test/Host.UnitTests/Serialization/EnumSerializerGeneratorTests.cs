namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using NSubstitute;
    using Xunit;

    public class EnumSerializerGeneratorTests
    {
        private readonly EnumSerializerGenerator generator;
        private readonly ModuleBuilder module;

        protected EnumSerializerGeneratorTests()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnitTestDynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            this.module = assemblyBuilder.DefineDynamicModule("Module");
            this.generator = new EnumSerializerGenerator(this.module, typeof(FakeSerializerBase));
        }

        public enum ShortEnum : short
        {
            Value = 1
        }

        private FakeSerializerBase SerializeArray(Array array, Type type)
        {
            object instance = Activator.CreateInstance(type, Stream.Null, SerializationMode.Serialize);

            ((ITypeSerializer)instance).WriteArray(array);
            return (FakeSerializerBase)instance;
        }

        private FakeSerializerBase SerializeValue(object value, Type type)
        {
            object instance = Activator.CreateInstance(type, Stream.Null, SerializationMode.Serialize);

            ((ITypeSerializer)instance).Write(value);
            return (FakeSerializerBase)instance;
        }

        public sealed class GenerateStringSerializer : EnumSerializerGeneratorTests
        {
            [Fact]
            public void ShouldSerializeArraysOfEnums()
            {
                FakeSerializerBase result = this.SerializeArray(
                    new ShortEnum[] { ShortEnum.Value });

                result.Writer.Received().WriteString(nameof(ShortEnum.Value));
            }

            [Fact]
            public void ShouldSerializeEnums()
            {
                FakeSerializerBase result = this.SerializeValue(ShortEnum.Value);

                result.Writer.Received().WriteString(nameof(ShortEnum.Value));
            }

            [Fact]
            public void ShouldSerializeNullableArraysOfEnums()
            {
                FakeSerializerBase result = this.SerializeArray(
                    new ShortEnum?[] { ShortEnum.Value, null });

                Received.InOrder(() =>
                {
                    result.Writer.WriteString(nameof(ShortEnum.Value));
                    result.Writer.WriteNull();
                });
            }

            [Fact]
            public void ShouldSerializeNullableEnums()
            {
                ShortEnum? value = ShortEnum.Value;
                FakeSerializerBase result = this.SerializeValue(value);

                result.Writer.Received().WriteString(nameof(ShortEnum.Value));
            }

            private FakeSerializerBase SerializeArray<T>(T[] array)
            {
                return this.SerializeArray(array, this.generator.GenerateStringSerializer(typeof(T)));
            }

            private FakeSerializerBase SerializeValue<T>(T value)
            {
                return this.SerializeValue(value, this.generator.GenerateStringSerializer(typeof(T)));
            }
        }

        public sealed class GenerateValueSerializer : EnumSerializerGeneratorTests
        {
            [Fact]
            public void ShouldSerializeArraysOfEnums()
            {
                FakeSerializerBase result = this.SerializeArray(
                    new ShortEnum[] { ShortEnum.Value });

                result.Writer.Received().WriteInt16((short)ShortEnum.Value);
            }

            [Fact]
            public void ShouldSerializeEnums()
            {
                FakeSerializerBase result = this.SerializeValue(ShortEnum.Value);

                result.Writer.Received().WriteInt16((short)ShortEnum.Value);
            }

            [Fact]
            public void ShouldSerializeNullableArraysOfEnums()
            {
                FakeSerializerBase result = this.SerializeArray(
                    new ShortEnum?[] { ShortEnum.Value, null });

                Received.InOrder(() =>
                {
                    result.Writer.WriteInt16((short)ShortEnum.Value);
                    result.Writer.WriteNull();
                });
            }

            [Fact]
            public void ShouldSerializeNullableEnums()
            {
                ShortEnum? value = ShortEnum.Value;
                FakeSerializerBase result = this.SerializeValue(value);

                result.Writer.Received().WriteInt16((short)ShortEnum.Value);
            }

            private FakeSerializerBase SerializeArray<T>(T[] array)
            {
                return this.SerializeArray(array, this.generator.GenerateValueSerializer(typeof(T)));
            }

            private FakeSerializerBase SerializeValue<T>(T value)
            {
                return this.SerializeValue(value, this.generator.GenerateValueSerializer(typeof(T)));
            }
        }
    }
}
