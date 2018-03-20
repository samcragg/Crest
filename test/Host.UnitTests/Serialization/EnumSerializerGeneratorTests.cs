namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
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

        private Array DeserializeArray<TValue>(Type type, Func<ValueReader, TValue> readMethod, params TValue[] values)
        {
            var serializer = (FakeSerializerBase)Activator.CreateInstance(
                type,
                Stream.Null,
                SerializationMode.Deserialize);

            serializer.SetArray(readMethod, values);

            return ((ITypeSerializer)serializer).ReadArray();
        }

        private object DeserializeValue<TValue>(Type type, Func<ValueReader, TValue> readMethod, TValue value)
        {
            var serializer = (FakeSerializerBase)Activator.CreateInstance(
                type,
                Stream.Null,
                SerializationMode.Deserialize);

            if (value == null)
            {
                serializer.Reader.ReadNull().Returns(true);
            }
            else
            {
                readMethod(serializer.Reader).Returns(value);
            }

            return ((ITypeSerializer)serializer).Read();
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
            public void ShouldDeserializeArraysOfEnums()
            {
                Array result = this.DeserializeArray<ShortEnum>(nameof(ShortEnum.Value));

                result.Should().Equal(ShortEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeEnums()
            {
                object result = this.DeserializeValue<ShortEnum>(nameof(ShortEnum.Value));

                result.Should().BeOfType<ShortEnum>().And.Be(ShortEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeNullableArraysOfEnums()
            {
                Array result = this.DeserializeArray<ShortEnum?>(nameof(ShortEnum.Value), null);

                result.Should().Equal(ShortEnum.Value, null);
            }

            [Fact]
            public void ShouldDeserializeNullableEnums()
            {
                object result = this.DeserializeValue<ShortEnum?>(nameof(ShortEnum.Value));

                // Nullables get boxed as their underlying type
                result.Should().BeOfType<ShortEnum>().And.Be(ShortEnum.Value);
            }

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

            private Array DeserializeArray<T>(params string[] values)
            {
                return this.DeserializeArray(
                    this.generator.GenerateStringSerializer(typeof(T)),
                    r => r.ReadString(),
                    values);
            }

            private object DeserializeValue<T>(string value)
            {
                return this.DeserializeValue(
                    this.generator.GenerateStringSerializer(typeof(T)),
                    r => r.ReadString(),
                    value);
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
            public void ShouldDeerializeNullableEnums()
            {
                object result = this.DeserializeValue<ShortEnum?>((short)ShortEnum.Value);

                // Nullables get boxed as their underlying type
                result.Should().BeOfType<ShortEnum>()
                      .And.Be(ShortEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeArraysOfEnums()
            {
                Array result = this.DeserializeArray<ShortEnum>((short)ShortEnum.Value);

                result.Should().Equal(ShortEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeEnums()
            {
                object result = this.DeserializeValue<ShortEnum>((short)ShortEnum.Value);

                result.Should().BeOfType<ShortEnum>().And.Be(ShortEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeNullableArraysOfEnums()
            {
                Array result = this.DeserializeArray<ShortEnum?>((short)ShortEnum.Value, null);

                result.Should().Equal(ShortEnum.Value, null);
            }

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

            private Array DeserializeArray<T>(params short?[] values)
            {
                return this.DeserializeArray(
                    this.generator.GenerateValueSerializer(typeof(T)),
                    r => r.ReadInt16(),
                    values);
            }

            private object DeserializeValue<T>(short? value)
            {
                return this.DeserializeValue(
                    this.generator.GenerateValueSerializer(typeof(T)),
                    r => r.ReadInt16(),
                    value);
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
