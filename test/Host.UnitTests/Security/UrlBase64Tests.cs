namespace Host.UnitTests.Security
{
    using Crest.Host.Security;
    using FluentAssertions;
    using Xunit;

    public class UrlBase64Tests
    {
        public sealed class Decode
        {
            [Fact]
            public void ShouldDecodeTheSubstring()
            {
                bool valid = UrlBase64.TryDecode("*Zg*", 1, 3, out byte[] result);

                valid.Should().BeTrue();
                result.Should().Equal((byte)'f');
            }

            // Data taken from https://tools.ietf.org/html/rfc4648#section-10
            // and https://tools.ietf.org/html/rfc7515#appendix-C
            [Theory]
            [InlineData("Zg", new[] { (byte)'f' })]
            [InlineData("Zm8", new[] { (byte)'f', (byte)'o' })]
            [InlineData("Zm9v", new[] { (byte)'f', (byte)'o', (byte)'o' })]
            [InlineData("Zm9vYg", new[] { (byte)'f', (byte)'o', (byte)'o', (byte)'b' })]
            [InlineData("Zm9vYmE", new[] { (byte)'f', (byte)'o', (byte)'o', (byte)'b', (byte)'a' })]
            [InlineData("Zm9vYmFy", new[] { (byte)'f', (byte)'o', (byte)'o', (byte)'b', (byte)'a', (byte)'r' })]
            [InlineData("A-z_4ME", new byte[] { 3, 236, 255, 224, 193 })]
            public void ShouldDecodeValues(string input, byte[] expected)
            {
                UrlBase64.TryDecode(input, 0, input.Length, out byte[] decoded);

                decoded.Should().Equal(expected);
            }

            [Fact]
            public void ShouldHandleEmptyStrings()
            {
                UrlBase64.TryDecode(string.Empty, 0, 0, out byte[] result);

                result.Should().BeEmpty();
            }

            [Theory]
            [InlineData("a!")]
            [InlineData("a£")]
            public void ShouldReturnFalseForInvalidCharacters(string value)
            {
                bool result = UrlBase64.TryDecode(value, 0, value.Length, out _);

                result.Should().BeFalse();
            }
        }
    }
}
