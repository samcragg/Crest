namespace Host.UnitTests.Serialization
{
    using System;
    using System.Linq;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class DateTimeConverterTests
    {
        public sealed class WriteDateTime : DateTimeConverterTests
        {
            [Theory]
            [InlineData("2000-01-02T03:04:05Z")]
            [InlineData("2000-12-13T14:15:16Z")]
            [InlineData("2000-12-13T14:15:16.1230000Z")]
            public void ShouldWriteAnIso8601DateTime(string value)
            {
                byte[] buffer = new byte[DateTimeConverter.MaximumTextLength];
                var dateTime = DateTime.Parse(value);

                int length = DateTimeConverter.WriteDateTime(buffer, 0, dateTime);

                buffer.Take(length).Should().Equal(Encoding.ASCII.GetBytes(value));
            }

            [Fact]
            public void ShouldWriteAtTheSpecifiedOffset()
            {
                byte[] buffer = new byte[DateTimeConverter.MaximumTextLength + 1];
                var dateTime = new DateTime(2017, 1, 1);

                DateTimeConverter.WriteDateTime(buffer, 1, dateTime);

                buffer.Should().StartWith(new byte[] { 0, (byte)'2' });
            }
        }
    }
}
