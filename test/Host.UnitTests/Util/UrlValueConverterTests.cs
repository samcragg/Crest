namespace Host.UnitTests.Util
{
    using System;
    using System.Linq.Expressions;
    using Crest.Host;
    using Crest.Host.Util;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class UrlValueConverterTests
    {
        private readonly UrlValueConverter instance = new UrlValueConverter();

        private string ConvertValue(object value)
        {
            ParameterExpression array = Expression.Parameter(typeof(object[]));
            ParameterExpression buffer = Expression.Parameter(typeof(StringBuffer));
            Expression expression = this.instance.AppendValue(buffer, array, 0, value.GetType());

            using (var stringBuffer = new StringBuffer())
            {
                Expression.Lambda(expression, buffer, array)
                    .Compile().DynamicInvoke(stringBuffer, new[] { value });

                return stringBuffer.ToString();
            }
        }

        public sealed class AppendValue : UrlValueConverterTests
        {
            [Fact]
            public void ShouldConvertDateValues()
            {
                string result = this.ConvertValue(new DateTime(1234, 1, 2, 3, 4, 5, DateTimeKind.Utc));

                result.Should().BeEquivalentTo("1234-01-02T03:04:05Z");
            }

            [Fact]
            public void ShouldConvertDecimalValues()
            {
                string result = this.ConvertValue(1.5m);

                result.Should().Be("1.5");
            }

            [Fact]
            public void ShouldConvertGuidValues()
            {
                const string GuidValue = "87B52FFB-BE31-480C-8120-578DBBC49842";

                string result = this.ConvertValue(Guid.Parse(GuidValue));

                result.Should().BeEquivalentTo(GuidValue);
            }

            [Theory]
            [InlineData(false, "false")]
            [InlineData((byte)1, "1")]
            [InlineData('X', "X")]
            [InlineData(1.5, "1.5")]
            [InlineData(1.5f, "1.5")]
            [InlineData((int)-1, "-1")]
            [InlineData((long)-1, "-1")]
            [InlineData((sbyte)-1, "-1")]
            [InlineData((short)-1, "-1")]
            [InlineData((uint)1, "1")]
            [InlineData((ulong)1, "1")]
            [InlineData((ushort)1, "1")]
            public void ShouldConvertPrimitiveTypes(object value, string expected)
            {
                string result = this.ConvertValue(value);

                result.Should().Be(expected);
            }

            [Fact]
            public void ShouldConvertStringValues()
            {
                string result = this.ConvertValue("A B");

                result.Should().Be("A%20B");
            }

            [Fact]
            public void ShouldConvertTimeSpanValues()
            {
                string result = this.ConvertValue(TimeSpan.FromMinutes(1));

                result.Should().BeEquivalentTo("PT1M");
            }

            [Fact]
            public void ShouldConvertUriAbsoluteValues()
            {
                string result = this.ConvertValue(new Uri("ab:c.d", UriKind.Absolute));

                result.Should().Be("ab%3Ac.d");
            }

            [Fact]
            public void ShouldConvertUriRelativeValues()
            {
                string result = this.ConvertValue(new Uri("a/b", UriKind.Relative));

                result.Should().Be("a%2Fb");
            }

            [Fact]
            public void ShouldConvertObjectsWithCustomTypeConverters()
            {
                object testValue = new ClassWithCustomTypeConverter("Example Text");

                string result = this.ConvertValue(testValue);

                result.Should().Be("Example%20Text");
            }
        }
    }
}
