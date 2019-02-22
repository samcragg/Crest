namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System.Text;
    using Crest.Host.Serialization.UrlEncoded;
    using FluentAssertions;
    using Xunit;

    public class UrlStringEncodingTests
    {
        private readonly byte[] buffer = new byte[UrlStringEncoding.MaxBytesPerCharacter];

        public sealed class AppendChar_Char : UrlStringEncodingTests
        {
            [Fact]
            public void ShouldEscapeReservedChars()
            {
                int count = UrlStringEncoding.AppendChar(default, '%', this.buffer);

                count.Should().Be(3);
                this.buffer.Should().HaveElementAt(0, (byte)'%');
                this.buffer.Should().HaveElementAt(1, (byte)'2');
                this.buffer.Should().HaveElementAt(2, (byte)'5');
            }

            [Fact]
            public void ShouldEscapeSpacesAsPercent()
            {
                int count = UrlStringEncoding.AppendChar(
                    UrlStringEncoding.SpaceOption.Percent,
                    ' ',
                    this.buffer);

                count.Should().Be(3);
                this.buffer.Should().HaveElementAt(0, (byte)'%');
                this.buffer.Should().HaveElementAt(1, (byte)'2');
                this.buffer.Should().HaveElementAt(2, (byte)'0');
            }

            [Fact]
            public void ShouldEscapeSpacesAsPlus()
            {
                int count = UrlStringEncoding.AppendChar(
                    UrlStringEncoding.SpaceOption.Plus,
                    ' ',
                    this.buffer);

                count.Should().Be(1);
                this.buffer.Should().HaveElementAt(0, (byte)'+');
            }

            // Values taken from https://tools.ietf.org/html/rfc3986#section-2.3
            [Theory]
            [InlineData('A')]
            [InlineData('Z')]
            [InlineData('a')]
            [InlineData('z')]
            [InlineData('0')]
            [InlineData('9')]
            [InlineData('-')]
            [InlineData('.')]
            [InlineData('_')]
            [InlineData('~')]
            public void ShouldNotEscapeUnreservedCharacters(char value)
            {
                int count = UrlStringEncoding.AppendChar(default, value, this.buffer);

                count.Should().Be(1);
                this.buffer.Should().HaveElementAt(0, (byte)value);
            }
        }

        public sealed class AppendChar_String : UrlStringEncodingTests
        {
            // Examples from Table 3-4 of the Unicode Standard 10.0
            [Theory]
            [InlineData("\u0040", "%40")]
            [InlineData("\u0430", "%D0%B0")]
            [InlineData("\u4e8c", "%E4%BA%8C")]
            [InlineData("\ud800\udf02", "%F0%90%8C%82")]
            public void ShouldEncodeUnicodeValues(string value, string bytes)
            {
                int index = 0;
                int count = UrlStringEncoding.AppendChar(default, value, ref index, this.buffer);

                // Check it advanced the index if necessary
                index.Should().Be(value.Length - 1);

                count.Should().Be(bytes.Length);
                this.buffer.Should().StartWith(Encoding.ASCII.GetBytes(bytes));
            }
        }
    }
}
