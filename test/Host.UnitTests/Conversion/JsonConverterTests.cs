﻿namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Conversion;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class JsonConverterTests
    {
        private JsonConverter converter;

        [SetUp]
        public void SetUp()
        {
            this.converter = new JsonConverter();
        }

        [Test]
        public void ContentTypeShouldBeTheIanaJsonMimeType()
        {
            // http://www.iana.org/assignments/media-types/application/json
            Assert.That(this.converter.ContentType, Is.EqualTo("application/json"));
        }

        [Test]
        public void FormatsShouldIncludeTheIanaJsonMimeType()
        {
            // http://www.iana.org/assignments/media-types/application/json
            Assert.That(this.converter.Formats, Has.Member("application/json"));
        }

        [Test]
        public void ProirityShouldReturnAPositiveNumber()
        {
            Assert.That(this.converter.Priority, Is.Positive);
        }

        [Test]
        public void WriteToMustNotDisposeTheStream()
        {
            Stream stream = Substitute.For<Stream>();
            stream.CanWrite.Returns(true);

            this.converter.WriteTo(stream, "value");

            stream.DidNotReceive().Dispose();
        }

        [Test]
        public void WriteToShouldEncodeWithUtf8()
        {
            using (var stream = new MemoryStream())
            {
                // ¶ (U+00B6) is a single UTF16 character but is encoded in
                // UTF-8 as 0xC2 0xB6
                this.converter.WriteTo(stream, new SimpleObject { StringProperty = "¶" });

                byte[] bytes = stream.ToArray();

                int start = Array.IndexOf(bytes, (byte)0xc2);
                Assert.That(bytes[start + 1], Is.EqualTo((byte)0xB6));
            }
        }

        [Test]
        public void WriteToShouldUseCamelCaseForProperties()
        {
            using (var stream = new MemoryStream())
            {
                this.converter.WriteTo(stream, new SimpleObject { IntegerProperty = 123 });

                string data = Encoding.UTF8.GetString(stream.ToArray());

                Assert.That(data, Contains.Substring("integerProperty"));
            }
        }

        private class SimpleObject
        {
            public int IntegerProperty { get; set; }

            public string StringProperty { get; set; }
        }
    }
}
