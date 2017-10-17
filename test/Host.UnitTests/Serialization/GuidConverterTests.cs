namespace Host.UnitTests.Serialization
{
    using System;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class GuidConverterTests
    {
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
