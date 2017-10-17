namespace Host.UnitTests.Serialization
{
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
                this.buffer[0].Should().Be((byte)'\\');
                this.buffer[1].Should().Be((byte)'u');
                this.buffer[2].Should().Be((byte)'0');
                this.buffer[3].Should().Be((byte)'0');
                this.buffer[4].Should().Be((byte)'1');
                this.buffer[5].Should().Be((byte)'2');
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
                this.buffer[0].Should().Be((byte)escaped[0]);
                this.buffer[1].Should().Be((byte)escaped[1]);
            }

            [Fact]
            public void ShouldOutputTheChar()
            {
                JsonStringEncoding.AppendChar('T', this.buffer, ref this.offset);

                this.offset.Should().Be(1);
                this.buffer[0].Should().Be((byte)'T');
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
    }
}
