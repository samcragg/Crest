namespace Host.UnitTests.Serialization.Json
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Json;
    using FluentAssertions;
    using Xunit;

    public class JsonStreamReaderTests
    {
        private static T ReadValue<T>(string input, Func<JsonStreamReader, T> readMethod)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                var reader = new JsonStreamReader(stream);
                return readMethod(reader);
            }
        }

        public sealed class ReadBoolean : JsonStreamReaderTests
        {
            [Fact]
            public void ShouldReturnFalseForTheFalseToken()
            {
                bool result = ReadValue("false", r => r.ReadBoolean());

                result.Should().Be(false);
            }

            [Fact]
            public void ShouldReturnTrueForTheTrueToken()
            {
                bool result = ReadValue("true", r => r.ReadBoolean());

                result.Should().Be(true);
            }

            [Fact]
            public void ShouldSkipLeadingWhiteSpace()
            {
                bool result = ReadValue(" true", r => r.ReadBoolean());

                result.Should().Be(true);
            }

            [Fact]
            public void ShouldThrowForInvalidTokens()
            {
                Action action = () => ReadValue("invalid", r => r.ReadBoolean());

                // Make sure the exception includes where it was
                action.Should().Throw<FormatException>().WithMessage("*1*");
            }
        }

        public sealed class ReadChar : JsonStreamReaderTests
        {
            [Fact]
            public void ShouldReadASingleCharacter()
            {
                char result = ReadValue("\"a\"", r => r.ReadChar());

                result.Should().Be('a');
            }

            [Fact]
            public void ShouldReadEscapedCharacters()
            {
                char result = ReadValue("\"\\\\\"", r => r.ReadChar());

                result.Should().Be('\\');
            }

            [Fact]
            public void ShouldThrowForInvalidTokens()
            {
                Action action = () => ReadValue("invalid", r => r.ReadChar());

                // Make sure the exception includes where it was
                action.Should().Throw<FormatException>().WithMessage("*1*");
            }
        }

        public sealed class ReadDateTime : JsonStreamReaderTests
        {
            [Fact]
            public void ShouldIgnoreWhitespace()
            {
                DateTime result = ReadValue("\" 2003-02-01 \"", r => r.ReadDateTime());

                result.Should().HaveYear(2003).And.HaveMonth(2).And.HaveDay(1);
            }
        }

        public sealed class ReadDecimal : JsonStreamReaderTests
        {
            [Theory]
            [InlineData("-79228162514264337593543950335")]
            [InlineData("0")]
            [InlineData("79228162514264337593543950335")]
            public void ShouldReadTheLimits(string value)
            {
                decimal expected = decimal.Parse(value, CultureInfo.InvariantCulture);

                decimal result = ReadValue(value, r => r.ReadDecimal());

                result.Should().Be(expected);
            }
        }

        public sealed class ReadDouble : JsonStreamReaderTests
        {
            [Theory]
            [InlineData(double.MinValue)]
            [InlineData(0)]
            [InlineData(double.MaxValue)]
            public void ShouldReadTheLimits(double value)
            {
                string stringValue = value.ToString("r", CultureInfo.InvariantCulture);

                double result = ReadValue(stringValue, r => r.ReadDouble());

                result.Should().Be(value);
            }
        }

        public sealed class ReadInt64 : JsonStreamReaderTests
        {
            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldReadIntegerLimits(long value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                long result = ReadValue(stringValue, r => r.ReadInt64());

                result.Should().Be(value);
            }
        }

        public sealed class ReadNull : JsonStreamReaderTests
        {
            [Fact]
            public void ShouldReturnFalseIfItIsNotTheNullToken()
            {
                bool result = ReadValue("123", r => r.ReadNull());

                result.Should().Be(false);
            }

            [Fact]
            public void ShouldReturnTrueForTheNullToken()
            {
                bool result = ReadValue("null", r => r.ReadNull());

                result.Should().Be(true);
            }

            [Fact]
            public void ShouldSkipLeadingWhiteSpace()
            {
                bool result = ReadValue(" null", r => r.ReadNull());

                result.Should().Be(true);
            }

            [Fact]
            public void ShouldThrowForInvalidTokens()
            {
                Action action = () => ReadValue("nothing", r => r.ReadNull());

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadString : JsonStreamReaderTests
        {
            [Fact]
            public void ShouldDecodeEscapedCharacters()
            {
                string result = ReadValue(@"""\/""", r => r.ReadString());

                result.Should().Be("/");
            }

            [Fact]
            public void ShouldReadEscapedQutotationMarks()
            {
                string result = ReadValue(@""" \"" """, r => r.ReadString());

                result.Should().Be(@" "" ");
            }

            [Fact]
            public void ShouldReadTheWholeString()
            {
                string result = ReadValue(@"""string""", r => r.ReadString());

                result.Should().Be("string");
            }

            [Fact]
            public void ShouldSkipLeadingWhiteSpace()
            {
                string result = ReadValue("\n\r\t \"a\"", r => r.ReadString());

                result.Should().Be("a");
            }

            [Fact]
            public void ShouldThrowIfThereIsNoEndOfStream()
            {
                Action action = () => ReadValue(@" ""not closed", r => r.ReadString());

                // Check the error message includes the start of the string (2)
                action.Should().Throw<FormatException>().WithMessage("*missing*2*");
            }
        }

        public sealed class ReadUInt64 : JsonStreamReaderTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldReadIntegerLimits(ulong value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                ulong result = ReadValue(stringValue, r => r.ReadUInt64());

                result.Should().Be(value);
            }
        }
    }
}
