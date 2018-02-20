namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class PrimitiveSerializerGeneratorTests
    {
        private readonly PrimitiveSerializerGenerator generator;

        public PrimitiveSerializerGeneratorTests()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnitTestDynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            this.generator = new PrimitiveSerializerGenerator(
                assemblyBuilder.DefineDynamicModule("Module"),
                typeof(_FakeBaseClass));
        }

        // Must be public for the generated classes to inherit from
        public class _FakeBaseClass : IClassSerializer<string>
        {
            protected _FakeBaseClass(Stream stream, SerializationMode mode)
            {
            }

            public ValueWriter Writer { get; } = Substitute.For<ValueWriter>();

            internal string BeginWriteMetadata { get; private set; }

            internal int EndWriteCount { get; private set; }

            public static string GetMetadata()
            {
                return null;
            }

            public static string GetTypeMetadata(Type type)
            {
                return type.Name;
            }

            public void BeginWrite(string type)
            {
                this.BeginWriteMetadata = type;
            }

            public void EndWrite()
            {
                this.EndWriteCount++;
            }

            public void Flush()
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

        public sealed class GetSerializers : PrimitiveSerializerGeneratorTests
        {
            [Fact]
            public void ShouldReturnSerializersForNullableTypes()
            {
                IEnumerable<KeyValuePair<Type, Type>> result =
                    this.generator.GetSerializers();

                result.Should().Contain(kvp => kvp.Key == typeof(int?));
            }

            [Fact]
            public void ShouldReturnSerializersForPrimitiveTypes()
            {
                IEnumerable<KeyValuePair<Type, Type>> result =
                    this.generator.GetSerializers();

                result.Should().Contain(kvp => kvp.Key == typeof(int));
            }

            [Fact]
            public void TheGeneratedSerializersShouldCallTheBeginWriteMethod()
            {
                object serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Write("value");

                ((_FakeBaseClass)serializer).BeginWriteMetadata.Should().Be(nameof(String));
            }

            [Fact]
            public void TheGeneratedSerializersShouldCallTheEndWriteMethod()
            {
                object serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Write("value");

                ((_FakeBaseClass)serializer).EndWriteCount.Should().Be(1);
            }

            [Fact]
            public void TheGeneratedSerializersShouldWriteArrays()
            {
                object serializer = this.GetSerializerFor<int>();

                ((ITypeSerializer)serializer).WriteArray(new int[] { 123 });

                ((_FakeBaseClass)serializer).Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void TheGeneratedSerializersShouldWriteNullableArrays()
            {
                object serializer = this.GetSerializerFor<int?>();

                ((ITypeSerializer)serializer).WriteArray(new int?[] { 123, null });

                ValueWriter writer = ((_FakeBaseClass)serializer).Writer;
                Received.InOrder(() =>
                {
                    writer.WriteInt32(123);
                    writer.WriteNull();
                });
            }

            [Fact]
            public void TheGeneratedSerializersShouldWriteNullableTypes()
            {
                object serializer = this.GetSerializerFor<short?>();

                short? nullable = 321;
                ((ITypeSerializer)serializer).Write(nullable);

                ((_FakeBaseClass)serializer).Writer.Received().WriteInt16(321);
            }

            [Fact]
            public void TheGeneratedSerializersShouldWriteReferenceTypes()
            {
                object serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Write("value");

                ((_FakeBaseClass)serializer).Writer.Received().WriteString("value");
            }

            [Fact]
            public void TheGeneratedSerializersShouldWriteValueTypes()
            {
                object serializer = this.GetSerializerFor<long>();

                ((ITypeSerializer)serializer).Write(123L);

                ((_FakeBaseClass)serializer).Writer.Received().WriteInt64(123);
            }

            private object GetSerializerFor<T>()
            {
                Type serializerType =
                    this.generator.GetSerializers()
                        .Where(kvp => kvp.Key == typeof(T))
                        .Select(kvp => kvp.Value)
                        .Single();

                return Activator.CreateInstance(serializerType, Stream.Null, SerializationMode.Serialize);
            }
        }
    }
}
