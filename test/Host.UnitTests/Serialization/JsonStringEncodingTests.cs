namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class JsonStringEncodingTests
    {
        private readonly byte[] buffer = new byte[JsonStringEncoding.MaxBytesPerCharacter];
        private int offset;

        public sealed class AppendChar_Char : JsonStringEncodingTests
        {
            [Fact]
            public void ShouldEscapeControlCharacters()
            {
                JsonStringEncoding.AppendChar('\x12', this.buffer, ref this.offset);

                this.offset.Should().Be(6);
                this.buffer.Should().HaveElementAt(0, (byte)'\\');
                this.buffer.Should().HaveElementAt(1, (byte)'u');
                this.buffer.Should().HaveElementAt(2, (byte)'0');
                this.buffer.Should().HaveElementAt(3, (byte)'0');
                this.buffer.Should().HaveElementAt(4, (byte)'1');
                this.buffer.Should().HaveElementAt(5, (byte)'2');
            }

            // Examples taken from http://json.org/
            [Theory]
            [InlineData('\"', "\\\"")]
            [InlineData('\\', "\\\\")]
            [InlineData('\b', "\\b")]
            [InlineData('\f', "\\f")]
            [InlineData('\n', "\\n")]
            [InlineData('\r', "\\r")]
            [InlineData('\t', "\\t")]
            public void ShouldEscapeReservedCharacters(char value, string escaped)
            {
                JsonStringEncoding.AppendChar(value, this.buffer, ref this.offset);

                this.offset.Should().Be(2);
                this.buffer.Should().HaveElementAt(0, (byte)escaped[0]);
                this.buffer.Should().HaveElementAt(1, (byte)escaped[1]);
            }

            [Fact]
            public void ShouldOutputTheChar()
            {
                JsonStringEncoding.AppendChar('T', this.buffer, ref this.offset);

                this.offset.Should().Be(1);
                this.buffer.Should().HaveElementAt(0, (byte)'T');
            }
        }

        public sealed class AppendChar_String : JsonStringEncodingTests
        {
            // Examples from Table 3-4 of the Unicode Standard 10.0
            [Theory]
            [InlineData("\u004d", new byte[] { 0x4d })]
            [InlineData("\u0430", new byte[] { 0xd0, 0xb0 })]
            [InlineData("\u4e8c", new byte[] { 0xe4, 0xba, 0x8c })]
            [InlineData("\ud800\udf02", new byte[] { 0xf0, 0x90, 0x8c, 0x82 })]
            public void ShouldEncodeUnicodeValues(string value, byte[] bytes)
            {
                int index = 0;
                JsonStringEncoding.AppendChar(value, ref index, this.buffer, ref this.offset);

                // Check it advanced the index if necessary
                index.Should().Be(value.Length - 1);

                this.offset.Should().Be(bytes.Length);
                this.buffer.Should().StartWith(bytes);
            }
        }

        public sealed class DecodeChar : JsonStringEncodingTests
        {
            [Theory]
            [InlineData(@"\""", '\"')]
            [InlineData(@"\\", '\\')]
            [InlineData(@"\/", '/')]
            [InlineData(@"\b", '\b')]
            [InlineData(@"\f", '\f')]
            [InlineData(@"\n", '\n')]
            [InlineData(@"\r", '\r')]
            [InlineData(@"\t", '\t')]
            public void ShouldDecodeEscapeSequences(string input, char expected)
            {
                char result = JsonStringEncoding.DecodeChar(CreateStreamIterator(input));

                result.Should().Be(expected);
            }

            [Theory]
            [InlineData(@"\u005C", '\\')]
            [InlineData(@"\u26aB", '⚫')]
            public void ShouldDecodeHexCharacters(string input, char expected)
            {
                char result = JsonStringEncoding.DecodeChar(CreateStreamIterator(input));

                result.Should().Be(expected);
            }

            [Fact]
            public void ShouldRejectInvalidEscapeSequences()
            {
                // Check the position is included, which is the position of the
                // start of the sequence (*0*)
                ExpectFormatException(@"\X", "*escape*0*");
            }

            [Fact]
            public void ShouldRejectInvalidHexidecimalValues()
            {
                // Check the position is included, which is the position of
                // the invalid hex character (*3*)
                ExpectFormatException(@"\uXXXX", "*hex*3*");
            }

            [Fact]
            public void ShouldRejectMissingEscapeSequences()
            {
                ExpectFormatException(@"\", "*end*");
            }

            [Fact]
            public void ShouldRejectMissingHexidecimalValues()
            {
                ExpectFormatException(@"\u123", "*end*");
            }

            [Fact]
            public void ShouldReturnUnescapedCharacters()
            {
                char result = JsonStringEncoding.DecodeChar(CreateStreamIterator("x"));

                result.Should().Be('x');
            }

            private static StreamIterator CreateStreamIterator(string input)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                var iterator = new StreamIterator(new MemoryStream(bytes, writable: false));

                // The iterator has to point to the position we want to start
                // decoding
                iterator.MoveNext();
                return iterator;
            }

            private static void ExpectFormatException(string input, string message)
            {
                Action action = () => JsonStringEncoding.DecodeChar(CreateStreamIterator(input));

                action.Should().Throw<FormatException>().WithMessage(message);
            }
        }
    }
}
