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
        public class _FakeBaseClass : FakeSerializerBase
        {
            protected _FakeBaseClass(Stream stream, SerializationMode mode)
            {
            }

            internal string BeginReadMetadata { get; private set; }

            internal string BeginWriteMetadata { get; private set; }

            internal int EndReadCount { get; private set; }

            internal int EndWriteCount { get; private set; }

            public override void BeginRead(string type)
            {
                this.BeginReadMetadata = type;
            }

            public override void BeginWrite(string type)
            {
                this.BeginWriteMetadata = type;
            }

            public override void EndRead()
            {
                this.EndReadCount++;
            }

            public override void EndWrite()
            {
                this.EndWriteCount++;
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
            public void TheGeneratedSerializersShouldCallTheBeginReadMethod()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Read();

                serializer.BeginReadMetadata.Should().Be(nameof(String));
            }

            [Fact]
            public void TheGeneratedSerializersShouldCallTheBeginWriteMethod()
            {
                object serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Write("value");

                ((_FakeBaseClass)serializer).BeginWriteMetadata.Should().Be(nameof(String));
            }

            [Fact]
            public void TheGeneratedSerializersShouldCallTheEndReadMethod()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Read();

                serializer.EndReadCount.Should().Be(1);
            }

            [Fact]
            public void TheGeneratedSerializersShouldCallTheEndWriteMethod()
            {
                object serializer = this.GetSerializerFor<string>();

                ((ITypeSerializer)serializer).Write("value");

                ((_FakeBaseClass)serializer).EndWriteCount.Should().Be(1);
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadArrays()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<int>();
                serializer.SetArray(r => r.ReadInt32(), 123);

                Array result = ((ITypeSerializer)serializer).ReadArray();

                result.Should().BeOfType<int[]>().Which.Should().Equal(123);
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadNullableArrays()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<int?>();
                serializer.SetArray(r => r.ReadInt32(), 123);

                Array result = ((ITypeSerializer)serializer).ReadArray();

                result.Should().BeOfType<int?[]>().Which.Should().Equal(123);
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadNullableTypes()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<short?>();
                serializer.Reader.ReadInt16().Returns((short)123);

                object result = ((ITypeSerializer)serializer).Read();

                // When a nullable gets boxed to an object, it get boxed as the
                // underlying type (if it has a value), hence the usage of
                // short here and not short?
                result.Should().BeOfType<short>().Which.Equals(123);
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadNullNullableTypes()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<short?>();
                serializer.Reader.ReadNull().Returns(true);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeNull();
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadNullReferenceTypes()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<string>();
                serializer.Reader.ReadNull().Returns(true);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeNull();
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadReferenceTypes()
            {
                _FakeBaseClass serializer = this.GetSerializerFor<string>();
                serializer.Reader.ReadString().Returns("value");

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<string>().Which.Equals("value");
            }

            [Fact]
            public void TheGeneratedSerializersShouldReadValueTypes()
            {
                object serializer = this.GetSerializerFor<long>();
                ((_FakeBaseClass)serializer).Reader.ReadInt64().Returns(123L);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<long>().Which.Equals(123);
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

            private _FakeBaseClass GetSerializerFor<T>()
            {
                Type serializerType =
                    this.generator.GetSerializers()
                        .Where(kvp => kvp.Key == typeof(T))
                        .Select(kvp => kvp.Value)
                        .Single();

                return (_FakeBaseClass)Activator.CreateInstance(serializerType, Stream.Null, SerializationMode.Serialize);
            }
        }
    }
}
