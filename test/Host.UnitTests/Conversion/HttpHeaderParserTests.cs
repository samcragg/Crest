namespace Host.UnitTests.Conversion
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Conversion;
    using Crest.Host.IO;
    using FluentAssertions;
    using Xunit;

    public class HttpHeaderParserTests
    {
        private const string NewLine = "\r\n";

        public sealed class Dispose : HttpHeaderParserTests
        {
            [Fact]
            public void ShouldDisposeTheStringBuffer()
            {
                FakeArrayPool<char> charPool = FakeArrayPool<char>.Instance;
                lock (FakeArrayPool.LockObject)
                {
                    charPool.Reset();

                    var parser = new HttpHeaderParser(new StringIterator("Field:value" + NewLine));
                    parser.ReadPairs();
                    charPool.TotalAllocated.Should().BeGreaterThan(0);

                    parser.Dispose();
                    charPool.TotalAllocated.Should().Be(0);
                }
            }
        }

        public sealed class ReadPairs : HttpHeaderParserTests
        {
            [Fact]
            public void ShouldAllowEmptyHeaders()
            {
                List<KeyValuePair<string, string>> result =
                    ParseHeader(string.Empty);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldNotAllowInvalidEscapedCharacters()
            {
                Action action = () => ParseHeader("Field: \"\\\n\" \r\n");

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldNotAllowInvalidNewLines()
            {
                Action action = () => ParseHeader("Field:value\n");

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldNotAllowInvalidQuotedCharacters()
            {
                Action action = () => ParseHeader("Field: \"\n\" \r\n");

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldNotAllowInvalidTokens()
            {
                Action action = () => ParseHeader("[]: value\r\n");

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldNotAllowMissingFieldValueSeparators()
            {
                Action action = () => ParseHeader("Field_value" + NewLine);

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldNotAllowMissingQuotations()
            {
                Action action = () => ParseHeader("Field: \"value");

                action.Should().Throw<FormatException>();
            }

            [Theory]
            [InlineData('"')]
            [InlineData(' ')]
            [InlineData('#')]
            [InlineData('[')]
            [InlineData(']')]
            [InlineData('~')]
            public void ShouldReadEscapedQuotedPairs(char value)
            {
                List<KeyValuePair<string, string>> result =
                    ParseHeader("Field: \"\\" + value + "\"" + NewLine);

                result.Should().ContainSingle().Which
                      .Value.Should().Be(value.ToString());
            }

            [Fact]
            public void ShouldReadMultipleFields()
            {
                List<KeyValuePair<string, string>> result =
                    ParseHeader("Field1: value" + NewLine + "Field2: value" + NewLine);

                result.Should().HaveCount(2);
                result[0].Key.Should().Be("Field1");
                result[1].Key.Should().Be("Field2");
            }

            [Fact]
            public void ShouldReadQuotedFieldValues()
            {
                List<KeyValuePair<string, string>> result =
                    ParseHeader("Field: \"field value\"" + NewLine);

                result.Should().ContainSingle().Which
                      .Value.Should().Be("field value");
            }

            [Fact]
            public void ShouldReadSpacesInsideFieldValues()
            {
                List<KeyValuePair<string, string>> result =
                    ParseHeader("Field: field value " + NewLine);

                result.Should().ContainSingle().Which
                      .Value.Should().Be("field value");
            }

            private static List<KeyValuePair<string, string>> ParseHeader(string value)
            {
                var parser = new HttpHeaderParser(new StringIterator(value));
                return new List<KeyValuePair<string, string>>(
                    parser.ReadPairs());
            }
        }

        public sealed class ReadParameter : HttpHeaderParserTests
        {
            [Fact]
            public void ShouldReadAttributeValues()
            {
                bool result = ReadAttributeValue(
                    "attribute=value",
                    out string attribute,
                    out string value);

                result.Should().BeTrue();
                attribute.Should().Be("attribute");
                value.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnFalseIfThereIsNoSeparator()
            {
                bool result = ReadAttributeValue(
                    "attribute",
                    out string attribute,
                    out string value);

                result.Should().BeFalse();
                attribute.Should().BeNull();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldUnescapeQuotedStrings()
            {
                bool result = ReadAttributeValue(
                    @"attribute=""value\\""",
                    out _,
                    out string value);

                result.Should().BeTrue();
                value.Should().Be(@"value\");
            }

            private static bool ReadAttributeValue(string text, out string attribute, out string value)
            {
                var parser = new HttpHeaderParser(text);
                return parser.ReadParameter(out attribute, out value);
            }
        }
    }
}
