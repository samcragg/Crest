namespace Host.UnitTests.Serialization
{
    using System;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class GuidConverterTests
    {
        public sealed class TryReadGuid : GuidConverterTests
        {
            [Theory]
            [InlineData("12345678-1234-1234")]
            [InlineData("12345678-1234-1234-1234-12345678901")]
            [InlineData("12345678-1234-1234-1234123456789012")]
            [InlineData("12345678-1234-1234-12340123456789012")]
            [InlineData("12345678-1234-123401234-123456789012")]
            [InlineData("12345678-123401234-1234-123456789012")]
            [InlineData("1234567801234-1234-1234-123456789012")]
            [InlineData("{12345678-1234-1234-1234-123456789012)")]
            [InlineData("+12345678-1234-1234-1234-123456789012+")]
            public void ShouldNotReadInvalidFormattedGuids(string value)
            {
                ParseResult<Guid> result = GuidConverter.TryReadGuid(value.AsSpan());

                result.IsSuccess.Should().BeFalse();
            }

            [Fact]
            public void ShouldNotReadInvalidHexValues()
            {
                ParseResult<Guid> result = GuidConverter.TryReadGuid(
                    "ABCDEFGH-ijkl-MNOP-qrst-uvwxyz123456".AsSpan());

                result.IsSuccess.Should().BeFalse();
                result.Error.Should().MatchEquivalentOf("*hex*");
            }

            // These formats are taken from Guid.ToString https://msdn.microsoft.com/en-us/library/windows/apps/97af8hh4.aspx
            [Theory]
            [InlineData("637325b675c145c4aa64d905cf3f7a90")]
            [InlineData("637325b6-75c1-45c4-aa64-d905cf3f7a90")]
            [InlineData("{637325b6-75c1-45c4-aa64-d905cf3f7a90}")]
            [InlineData("(637325b6-75c1-45c4-aa64-d905cf3f7a90)")]
            public void ShouldReadValidGuidStringFormats(string value)
            {
                ParseResult<Guid> result = GuidConverter.TryReadGuid(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().Be(Guid.Parse(value));
                result.Length.Should().Be(value.Length);
            }
        }

        public sealed class WriteGuid : GuidConverterTests
        {
            private const string GuidString = "6FCBC911-2025-4D99-A9CF-D86CF1CC809C";

            [Fact]
            public void ShouldWriteAnHyphenatedGuid()
            {
                byte[] buffer = new byte[GuidConverter.MaximumTextLength];

                GuidConverter.WriteGuid(buffer, 0, new Guid(GuidString));

                Encoding.ASCII.GetString(buffer)
                        .Should().BeEquivalentTo(GuidString);
            }

            [Fact]
            public void ShouldWriteAtTheSpecifiedOffset()
            {
                byte[] buffer = new byte[GuidConverter.MaximumTextLength + 1];

                GuidConverter.WriteGuid(buffer, 1, new Guid(GuidString));

                buffer.Should().StartWith(new byte[] { 0, (byte)'6' });
            }
        }
    }
}
