namespace OpenApi.UnitTests
{
    using System.IO;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class JsonWriterTests
    {
        private readonly FakeJsonWriter writer = new FakeJsonWriter();

        public sealed class Write : JsonWriterTests
        {
            [Fact]
            public void ShouldEscapeTheData()
            {
                this.writer.Write(@"\");

                this.writer.Output.Should().Be(@"\\");
            }

            [Fact]
            public void WriteShouldWriteTheCharacter()
            {
                this.writer.Write('"');

                this.writer.Output.Should().Be("\"");
            }
        }

        public sealed class WriteRaw : JsonWriterTests
        {
            [Fact]
            public void ShouldNotEscapeTheData()
            {
                this.writer.WriteRaw(@"\");

                this.writer.Output.Should().Be(@"\");
            }
        }

        public sealed class WriteString : JsonWriterTests
        {
            [Fact]
            public void ShouldEscapeTheData()
            {
                this.writer.WriteString(@"\");

                this.writer.Output.Should().Be(@"""\\""");
            }
        }

        public sealed class WriteValue : JsonWriterTests
        {
            [Fact]
            public void ShouldWriteBooleanValues()
            {
                this.writer.WriteValue(false);
                this.writer.Write(' ');
                this.writer.WriteValue(true);

                this.writer.Output.Should().Be("false true");
            }

            [Fact]
            public void ShouldWriteNullValues()
            {
                this.writer.WriteValue(null);

                this.writer.Output.Should().Be("null");
            }

            [Fact]
            public void ShouldWriteNumbers()
            {
                this.writer.WriteValue(123);

                this.writer.Output.Should().Be("123");
            }

            [Fact]
            public void ShouldWriteStrings()
            {
                this.writer.WriteValue(@"test\");

                this.writer.Output.Should().Be(@"""test\\""");
            }
        }

        private class FakeJsonWriter : JsonWriter
        {
            private readonly StringWriter writer;

            public FakeJsonWriter()
                : this(new StringWriter())
            {
            }

            private FakeJsonWriter(StringWriter writer)
                : base(writer)
            {
                this.writer = writer;
            }

            internal string Output => this.writer.ToString();

            internal new void Write(char value)
            {
                base.Write(value);
            }

            internal new void Write(string value)
            {
                base.Write(value);
            }

            internal new void WriteRaw(string value)
            {
                base.WriteRaw(value);
            }

            internal new void WriteString(string value)
            {
                base.WriteString(value);
            }

            internal new void WriteValue(object value)
            {
                base.WriteValue(value);
            }
        }
    }
}
