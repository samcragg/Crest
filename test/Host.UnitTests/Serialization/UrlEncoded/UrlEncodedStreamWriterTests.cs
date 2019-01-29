namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedStreamWriterTests : IDisposable
    {
        private readonly byte[] keyValue = new byte[] { (byte)'k', (byte)'e', (byte)'y' };
        private readonly MemoryStream stream = new MemoryStream();
        private readonly UrlEncodedStreamWriter writer;

        protected UrlEncodedStreamWriterTests()
        {
            this.writer = new UrlEncodedStreamWriter(this.stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        private string GetString<T>(Action<T> write, T value)
        {
            this.stream.SetLength(0);
            write(value);
            this.writer.Flush();
            return Encoding.UTF8.GetString(this.stream.ToArray());
        }

        public sealed class PopKeyPart : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldRemoveTheMostRecentlyAdded()
            {
                this.writer.PushKeyPart(1);
                this.writer.PushKeyPart(2);

                this.writer.PopKeyPart();
                string result = this.GetString(this.writer.WriteString, string.Empty);

                result.Should().Be("1=");
            }
        }

        public sealed class PushKeyPart : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldJoinPartsWithADot()
            {
                this.writer.PushKeyPart(1);
                this.writer.PushKeyPart(2);

                string result = this.GetString(this.writer.WriteString, string.Empty);

                result.Should().Be("1.2=");
            }

            [Fact]
            public void ShouldOutputArrayIndexes()
            {
                this.writer.PushKeyPart(12);

                string result = this.GetString(this.writer.WriteString, string.Empty);

                result.Should().Be("12=");
            }

            [Fact]
            public void ShouldOutputPropertyNames()
            {
                this.writer.PushKeyPart(new byte[] { (byte)'p' });

                string result = this.GetString(this.writer.WriteString, string.Empty);

                result.Should().Be("p=");
            }

            [Fact]
            public void ShouldWriteASeparatorBetweenKeyValues()
            {
                this.writer.PushKeyPart(1);
                this.writer.WriteString(string.Empty);

                string result = this.GetString(this.writer.WriteString, string.Empty);

                result.Should().Be("1=&1=");
            }
        }

        public sealed class WriteBoolean : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteFalse()
            {
                string result = this.GetString(this.writer.WriteBoolean, false);

                result.Should().Be("false");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string result = this.GetString(this.writer.WriteBoolean, true);

                result.Should().StartWith("key=");
            }

            [Fact]
            public void ShouldWriteTrue()
            {
                string result = this.GetString(this.writer.WriteBoolean, true);

                result.Should().Be("true");
            }
        }

        public sealed class WriteChar : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldEncodeTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, '+');

                result.Should().Be("%2B");
            }

            [Fact]
            public void ShouldOutputTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, 'T');

                result.Should().Be("T");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteChar, default(char));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteDateTime : UrlEncodedStreamWriterTests
        {
            // This tests the methods required by the base method to rent/commit
            // the buffer work. DateTime is chosen as it's simple and also
            // writes a smaller value than the buffer size requested.

            [Fact]
            public void ShouldTheValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, DateTimeKind.Utc);

                string result = this.GetString(this.writer.WriteDateTime, dateTime);

                result.Should().Be("2017-01-02T13:14:15Z");
            }
        }

        public sealed class WriteNull : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteNull()
            {
                this.writer.WriteNull();
                this.writer.Flush();
                string result = Encoding.UTF8.GetString(this.stream.ToArray());

                result.Should().Be("null");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);
                this.writer.WriteNull();

                this.writer.Flush();
                string result = Encoding.UTF8.GetString(this.stream.ToArray());

                result.Should().StartWith("key=");
            }
        }

        public sealed class WriteString : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldEncodeSpaces()
            {
                string result = this.GetString(this.writer.WriteString, "Test Data");

                result.Should().Be("Test+Data");
            }

            // Examples from Table 3-4 of the Unicode Standard 10.0
            [Theory]
            [InlineData("\u0040", "%40")]
            [InlineData("\u0430", "%D0%B0")]
            [InlineData("\u4e8c", "%E4%BA%8C")]
            [InlineData("\ud800\udf02", "%F0%90%8C%82")]
            public void ShouldEncodeUnicodeValues(string value, string escaped)
            {
                string result = this.GetString(this.writer.WriteString, value);

                result.Should().Be(escaped);
            }

            [Fact]
            public void ShouldOutputLongStrings()
            {
                // This tests the buffer gets flushed when it's full
                string longString = new string('a', 2000);

                string result = this.GetString(this.writer.WriteString, longString);

                result.Should().Be(longString);
            }

            [Fact]
            public void ShouldOutputTheString()
            {
                string result = this.GetString(this.writer.WriteString, "Test_*.-");

                result.Should().Be("Test_*.-");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteUri : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteAbsoluteUris()
            {
                var uri = new Uri("http://www.example.com/escaped%20path");

                string result = this.GetString(this.writer.WriteUri, uri);

                result.Should().Be(uri.AbsoluteUri);
            }

            [Fact]
            public void ShouldWriteLargeUris()
            {
                // The writer has a limited buffer, so ensure that it can handle
                // large values
                var uri = new Uri("http://www.example.com/" + new string('a', 2000));

                string result = this.GetString(this.writer.WriteUri, uri);

                result.Should().Be(uri.AbsoluteUri);
            }

            [Fact]
            public void ShouldWriteReltiveUris()
            {
                string result = this.GetString(
                    this.writer.WriteUri,
                    new Uri("needs escaping", UriKind.Relative));

                result.Should().Be("needs+escaping");
            }
        }
    }
}
