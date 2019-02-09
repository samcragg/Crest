namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using Crest.Host.Engine;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    // Because we're using statics in the FakeFormatter to monitor what gets
    // called, run all tests serially
    [Collection(nameof(DelegateAdapterTests))]
    public class DelegateAdapterTests
    {
        private readonly DelegateAdapter<FakeFormatter> adapter;

        private DelegateAdapterTests()
        {
            this.adapter = new DelegateAdapter<FakeFormatter>(
                new DiscoveredTypes(Array.Empty<Type>()));
        }

        public sealed class Deserialize : DelegateAdapterTests
        {
            [Fact]
            public void ShouldReadTheValueFromTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                FakeFormatter.ValueReader.ReadInt32().Returns(123);

                object result = this.adapter.Deserialize(stream, typeof(int));

                FakeFormatter.StreamPassedInToConstructor.Should().BeSameAs(stream);
                result.Should().Be(123);
            }
        }

        public sealed class Prime : DelegateAdapterTests
        {
            [Fact]
            public void ShouldCacheTheDelegates()
            {
                FakeFormatter.MetadataCount = 0;

                this.adapter.Prime(typeof(ClassWithSingleProperty));
                FakeFormatter.MetadataCount.Should().Be(1);

                this.adapter.Prime(typeof(ClassWithSingleProperty));
                FakeFormatter.MetadataCount.Should().Be(1);
            }

            private class ClassWithSingleProperty
            {
                public int Property { get; set; }
            }
        }

        public sealed class Serialize : DelegateAdapterTests
        {
            [Fact]
            public void ShouldFlushTheStream()
            {
                FakeFormatter.ValueWriter.ClearReceivedCalls();

                this.adapter.Serialize(Stream.Null, 123);

                FakeFormatter.ValueWriter.Received().Flush();
            }

            [Fact]
            public void ShouldWriteTheValueToTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                FakeFormatter.ValueWriter.ClearReceivedCalls();

                this.adapter.Serialize(stream, 123);

                FakeFormatter.StreamPassedInToConstructor.Should().BeSameAs(stream);
                FakeFormatter.ValueWriter.Received().WriteInt32(123);
            }
        }

        private class FakeFormatter : IFormatter
        {
            public FakeFormatter(Stream stream, SerializationMode mode)
            {
                StreamPassedInToConstructor = stream;
            }

            public bool EnumsAsIntegers { get; }

            public ValueReader Reader => ValueReader;

            public ValueWriter Writer => ValueWriter;

            internal static int MetadataCount { get; set; }

            internal static Stream StreamPassedInToConstructor { get; private set; }

            internal static ValueReader ValueReader { get; } = Substitute.For<ValueReader>();

            internal static ValueWriter ValueWriter { get; } = Substitute.For<ValueWriter>();

            public static object GetMetadata(PropertyInfo property)
            {
                MetadataCount++;
                return property;
            }

            public void ReadBeginPrimitive(object metadata)
            {
            }

            public void ReadEndPrimitive()
            {
            }

            public void WriteBeginPrimitive(object metadata)
            {
            }

            public void WriteEndPrimitive()
            {
            }

            bool IClassReader.ReadBeginArray(Type elementType)
            {
                throw new NotImplementedException();
            }

            void IFormatter.ReadBeginClass(object metadata)
            {
                throw new NotImplementedException();
            }

            void IClassReader.ReadBeginClass(string className)
            {
                throw new NotImplementedException();
            }

            string IClassReader.ReadBeginProperty()
            {
                throw new NotImplementedException();
            }

            bool IClassReader.ReadElementSeparator()
            {
                throw new NotImplementedException();
            }

            void IClassReader.ReadEndArray()
            {
                throw new NotImplementedException();
            }

            void IClassReader.ReadEndClass()
            {
                throw new NotImplementedException();
            }

            void IClassReader.ReadEndProperty()
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteBeginArray(Type elementType, int size)
            {
                throw new NotImplementedException();
            }

            void IFormatter.WriteBeginClass(object metadata)
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteBeginClass(string className)
            {
                throw new NotImplementedException();
            }

            void IFormatter.WriteBeginProperty(object metadata)
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteBeginProperty(string propertyName)
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteElementSeparator()
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteEndArray()
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteEndClass()
            {
                throw new NotImplementedException();
            }

            void IClassWriter.WriteEndProperty()
            {
                throw new NotImplementedException();
            }
        }
    }
}
