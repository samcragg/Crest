namespace Host.UnitTests.Serialization
{
    using System;
    using System.Text;
    using System.Xml;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class TimeSpanConverterTests
    {
        public sealed class WriteTimeSpan : TimeSpanConverterTests
        {
            [Fact]
            public void ShouldIncludeDays()
            {
                string result = GetString(TimeSpan.FromDays(12));

                result.Should().BeEquivalentTo("P12D");
            }

            [Fact]
            public void ShouldIncludeFractionOfSeconds()
            {
                string result = GetString(TimeSpan.FromTicks(123));

                result.Should().BeEquivalentTo("PT0.0000123S");
            }

            [Fact]
            public void ShouldIncludeHours()
            {
                string result = GetString(TimeSpan.FromHours(12));

                result.Should().BeEquivalentTo("PT12H");
            }

            [Fact]
            public void ShouldIncludeMinutes()
            {
                string result = GetString(TimeSpan.FromMinutes(12));

                result.Should().BeEquivalentTo("PT12M");
            }

            [Fact]
            public void ShouldIncludeSeconds()
            {
                string result = GetString(TimeSpan.FromSeconds(12));

                result.Should().BeEquivalentTo("PT12S");
            }

            [Fact]
            public void ShouldNotOutputLeadingZerosForTimeParts()
            {
                string result = GetString(new TimeSpan(1, 2, 3));

                result.Should().BeEquivalentTo("PT1H2M3S");
            }

            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteTheLimits(long ticks)
            {
                var time = TimeSpan.FromTicks(ticks);

                string result = GetString(time);

                result.Should().BeEquivalentTo(XmlConvert.ToString(time));
            }

            private static string GetString(TimeSpan value)
            {
                byte[] buffer = new byte[TimeSpanConverter.MaximumTextLength];
                int length = TimeSpanConverter.WriteTimeSpan(buffer, 0, value);
                return Encoding.UTF8.GetString(buffer, 0, length);
            }
        }
    }
}
