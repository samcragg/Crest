namespace Host.UnitTests.Serialization.Json
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class JsonStreamWriterTests : IDisposable
    {
        private readonly MemoryStream stream = new MemoryStream();
        private readonly JsonStreamWriter writer;

        protected JsonStreamWriterTests()
        {
            this.writer = new JsonStreamWriter(this.stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        private string GetString<T>(Action<T> write, T value)
        {
            this.stream.SetLength(0);
            write(value);
            this.writer.Flush();
            return Encoding.UTF8.GetString(this.stream.ToArray());
        }

        public sealed class WriteBoolean : JsonStreamWriterTests
        {
            [Fact]
            public void ShouldWriteFalse()
            {
                string result = this.GetString(this.writer.WriteBoolean, false);

                result.Should().Be("false");
            }

            [Fact]
            public void ShouldWriteTrue()
            {
                string result = this.GetString(this.writer.WriteBoolean, true);

                result.Should().Be("true");
            }
        }

        public sealed class WriteChar : JsonStreamWriterTests
        {
            [Fact]
            public void ShouldOutputTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, 'T');

                result.Should().Be("\"T\"");
            }
        }

        public sealed class WriteDateTime : JsonStreamWriterTests
        {
            // This tests the methods required by the base method to rent/commit
            // the buffer work. DateTime is chosen as it's simple and also
            // writes a smaller value than the buffer size requested.

            [Fact]
            public void ShouldTheValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, DateTimeKind.Utc);

                string result = this.GetString(this.writer.WriteDateTime, dateTime);

                result.Should().Be("\"2017-01-02T13:14:15Z\"");
            }
        }

        public sealed class WriteDecimal : JsonStreamWriterTests
        {
            [Fact]
            public void ShouldWriteTheNumber()
            {
                string result = this.GetString(this.writer.WriteDecimal, 123.4m);

                result.Should().BeEquivalentTo("123.4");
            }
        }

        public sealed class WriteDouble : JsonStreamWriterTests
        {
            [Theory]
            [InlineData(double.NaN)]
            [InlineData(double.NegativeInfinity)]
            [InlineData(double.PositiveInfinity)]
            public void ShouldNotAllowInvalidJsonValues(double value)
            {
                // JSON numbers can't be all the valid values of our good old
                // IEEE double precision floating point number :(
                this.writer.Invoking(w => w.WriteDouble(value))
                    .Should().Throw<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldWriteTheNumber()
            {
                string result = this.GetString(this.writer.WriteDouble, 123.4);

                result.Should().BeEquivalentTo("123.4");
            }
        }

        public sealed class WriteInt64 : JsonStreamWriterTests
        {
            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteIntegerLimits(long value)
            {
                string result = this.GetString(this.writer.WriteInt64, value);

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteNull : JsonStreamWriterTests
        {
            [Fact]
            public void ShouldWriteNull()
            {
                this.writer.WriteNull();
                this.writer.Flush();
                string result = Encoding.UTF8.GetString(this.stream.ToArray());

                result.Should().Be("null");
            }
        }

        public sealed class WriteSingle : JsonStreamWriterTests
        {
            [Theory]
            [InlineData(float.NaN)]
            [InlineData(float.NegativeInfinity)]
            [InlineData(float.PositiveInfinity)]
            public void ShouldNotAllowInvalidJsonValues(float value)
            {
                // See remarks in WriteDouble tests
                this.writer.Invoking(w => w.WriteSingle(value))
                    .Should().Throw<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldWriteTheNumber()
            {
                string result = this.GetString(this.writer.WriteSingle, 123.4f);

                result.Should().BeEquivalentTo("123.4");
            }
        }

        public sealed class WriteString : JsonStreamWriterTests
        {
            [Fact]
            public void ShouldOutputLongStrings()
            {
                // This tests the buffer gets flushed when it's full
                string longString = new string('a', 2000);

                string result = this.GetString(this.writer.WriteString, longString);

                // Remove the surrounding quotes
                result.Substring(1, result.Length - 2)
                      .Should().Be(longString);
            }

            [Fact]
            public void ShouldOutputTheString()
            {
                string result = this.GetString(this.writer.WriteString, @"Test\Data");

                result.Should().Be(@"""Test\\Data""");
            }
        }

        public sealed class WriteUInt64 : JsonStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldWriteIntegerLimits(ulong value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt64, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
