namespace Host.UnitTests.Serialization.Internal
{
    using System;
    using System.IO;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class CustomSerializerAdapterTests
    {
        private readonly CustomSerializerAdapter<SimpleType, FakeFormatter, FakeCustomSerializer> adapter;
        private readonly IClassReader reader;
        private readonly IClassWriter writer;

        private CustomSerializerAdapterTests()
        {
            var formatter = new FakeFormatter();

            this.reader = formatter.ClassReader;
            this.reader.Reader.Returns(Substitute.For<ValueReader>());

            this.writer = formatter.ClassWriter;
            this.writer.Writer.Returns(Substitute.For<ValueWriter>());

            this.adapter =
                new CustomSerializerAdapter<SimpleType, FakeFormatter, FakeCustomSerializer>(formatter);
        }

        public sealed class Constructor : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldBeConstructableFromAStreamAndSerializtionMode()
            {
                // This is required for the serializer generator
                Action action = () => new CustomSerializerAdapter<SimpleType, FakeFormatter, FakeCustomSerializer>(
                    Stream.Null, SerializationMode.Serialize);

                action.Should().NotThrow();
            }
        }

        public sealed class Flush : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldFlushTheWriter()
            {
                this.adapter.Flush();

                this.writer.Writer.Received().Flush();
            }
        }

        public sealed class Read : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldReadFromTheSerializer()
            {
                var value = new SimpleType();
                FakeCustomSerializer.SetReadValue(value);

                object result = this.adapter.Read();

                result.Should().BeSameAs(value);
            }
        }

        public sealed class ReadArray : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldCallReadEndArrayIfBeginArrayReturnsTrue()
            {
                this.reader.ReadBeginArray(typeof(SimpleType)).Returns(true);

                Array result = this.adapter.ReadArray();

                this.reader.Received().ReadEndArray();
                result.Should().HaveCount(1);
            }

            [Fact]
            public void ShouldDeserializeArrayItems()
            {
                var value = new SimpleType();
                FakeCustomSerializer.SetReadValue(value);

                this.reader.ReadBeginArray(typeof(SimpleType)).Returns(true);
                this.reader.ReadElementSeparator().Returns(true, false);
                this.reader.Reader.ReadNull().Returns(true, false);

                Array result = this.adapter.ReadArray();

                result.Should().Equal(null, value);
            }

            [Fact]
            public void ShouldNotCallReadEndArrayIfBeginArrayReturnsFalse()
            {
                this.reader.ReadBeginArray(typeof(SimpleType)).Returns(false);

                Array result = this.adapter.ReadArray();

                this.reader.DidNotReceive().ReadEndArray();
                result.Should().BeEmpty();
            }
        }

        public sealed class Write : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldWriteTheValueToTheSerializer()
            {
                var value = new SimpleType();

                this.adapter.Write(value);

                FakeCustomSerializer.GetLastWrittenValue().Should().BeSameAs(value);
            }
        }

        public sealed class WriteArray : CustomSerializerAdapterTests
        {
            [Fact]
            public void ShouldCallBeginWriteArray()
            {
                this.adapter.WriteArray(new SimpleType[12]);

                this.writer.Received().WriteBeginArray(typeof(SimpleType), 12);
            }

            [Fact]
            public void ShouldCallWriteElementSeparatorBetweenElements()
            {
                this.adapter.WriteArray(new SimpleType[3]);

                // [0] separator [1] separator [2]
                this.writer.Received(2).WriteElementSeparator();
            }

            [Fact]
            public void ShouldCallWriteEndArray()
            {
                this.adapter.WriteArray(new SimpleType[0]);

                this.writer.Received().WriteEndArray();
            }

            [Fact]
            public void ShouldNotCallWriteElementSeparatorForSingleElementArrays()
            {
                this.adapter.WriteArray(new SimpleType[1]);

                this.writer.DidNotReceive().WriteElementSeparator();
            }

            [Fact]
            public void ShouldWriteTheArrayElements()
            {
                var value = new SimpleType();

                this.adapter.WriteArray(new[] { null, value });

                this.writer.Writer.WriteNull();
                FakeCustomSerializer.GetLastWrittenValue().Should().BeSameAs(value);
            }
        }

        private sealed class FakeCustomSerializer : ICustomSerializer<SimpleType>
        {
            private static SimpleType lastValue;

            public SimpleType Read(IClassReader reader)
            {
                return GetLastWrittenValue();
            }

            public void Write(IClassWriter writer, SimpleType instance)
            {
                lastValue = instance;
            }

            internal static SimpleType GetLastWrittenValue()
            {
                SimpleType temp = lastValue;
                lastValue = null;
                return temp;
            }

            internal static void SetReadValue(SimpleType value)
            {
                lastValue = value;
            }
        }

        private class FakeFormatter : IClassReader, IClassWriter
        {
            public FakeFormatter()
            {
                this.ClassReader = Substitute.For<IClassReader>();
                this.ClassWriter = Substitute.For<IClassWriter>();
            }

            public FakeFormatter(Stream stream, SerializationMode mode)
            {
            }

            public ValueReader Reader => this.ClassReader.Reader;

            public ValueWriter Writer => this.ClassWriter.Writer;

            internal IClassReader ClassReader { get; }

            internal IClassWriter ClassWriter { get; }

            public bool ReadBeginArray(Type elementType)
            {
                return this.ClassReader.ReadBeginArray(elementType);
            }

            public void ReadBeginClass(string className)
            {
                this.ClassReader.ReadBeginClass(className);
            }

            public string ReadBeginProperty()
            {
                return this.ClassReader.ReadBeginProperty();
            }

            public bool ReadElementSeparator()
            {
                return this.ClassReader.ReadElementSeparator();
            }

            public void ReadEndArray()
            {
                this.ClassReader.ReadEndArray();
            }

            public void ReadEndClass()
            {
                this.ClassReader.ReadEndClass();
            }

            public void ReadEndProperty()
            {
                this.ClassReader.ReadEndProperty();
            }

            public void WriteBeginArray(Type elementType, int size)
            {
                this.ClassWriter.WriteBeginArray(elementType, size);
            }

            public void WriteBeginClass(string className)
            {
                this.ClassWriter.WriteBeginClass(className);
            }

            public void WriteBeginProperty(string propertyName)
            {
                this.ClassWriter.WriteBeginProperty(propertyName);
            }

            public void WriteElementSeparator()
            {
                this.ClassWriter.WriteElementSeparator();
            }

            public void WriteEndArray()
            {
                this.ClassWriter.WriteEndArray();
            }

            public void WriteEndClass()
            {
                this.ClassWriter.WriteEndClass();
            }

            public void WriteEndProperty()
            {
                this.ClassWriter.WriteEndProperty();
            }
        }

        private sealed class SimpleType
        {
        }
    }
}
