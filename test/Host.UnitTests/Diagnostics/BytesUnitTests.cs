namespace Host.UnitTests.Diagnostics
{
    using System.Globalization;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class BytesUnitTests
    {
        private readonly BytesUnit bytes;

        public BytesUnitTests()
        {
            this.bytes = new BytesUnit();
        }

        public sealed class Format : BytesUnitTests
        {
            [Fact]
            public void ShouldReturnBytesForSmallValues()
            {
                string result = this.bytes.Format(123);

                result.Should().Be("123 B");
            }

            [Fact]
            public void ShouldReturnGigiBytes()
            {
                string result = this.bytes.Format(13249974109L);

                result.Should().Be("12.34 GiB");
            }

            [Fact]
            public void ShouldReturnKibiBytes()
            {
                string result = this.bytes.Format(12640);

                result.Should().Be("12.34 KiB");
            }

            [Fact]
            public void ShouldReturnMebiBytes()
            {
                string result = this.bytes.Format(12939428);

                result.Should().Be("12.34 MiB");
            }

            [Fact]
            public void ShouldUseTheCurrentCulture()
            {
                CultureInfo.CurrentCulture = new CultureInfo("ES");
                string result = this.bytes.Format(12640);

                result.Should().Be("12,34 KiB");
            }
        }
    }
}
