﻿namespace OpenApi.UnitTests
{
    using System.IO;
    using Crest.OpenApi;
    using NUnit.Framework;

    [TestFixture]
    public sealed class JsonWriterTests
    {
        private FakeJsonWriter writer;

        [SetUp]
        public void SetUp()
        {
            this.writer = new FakeJsonWriter();
        }

        [Test]
        public void WriteShouldWriteTheCharacter()
        {
            this.writer.Write('"');

            Assert.That(this.writer.Output, Is.EqualTo("\""));
        }

        [Test]
        public void WriteShouldEscapeTheData()
        {
            this.writer.Write(@"\");

            Assert.That(this.writer.Output, Is.EqualTo(@"\\"));
        }

        [Test]
        public void WriteRawShouldNotEscapeTheData()
        {
            this.writer.WriteRaw(@"\");

            Assert.That(this.writer.Output, Is.EqualTo(@"\"));
        }

        [Test]
        public void WriteValueShouldWriteNullValues()
        {
            this.writer.WriteValue(null);

            Assert.That(this.writer.Output, Is.EqualTo("null"));
        }

        [Test]
        public void WriteValueShouldWriteBooleanValues()
        {
            this.writer.WriteValue(false);
            this.writer.Write(' ');
            this.writer.WriteValue(true);

            Assert.That(this.writer.Output, Is.EqualTo("false true"));
        }

        [Test]
        public void WriteValueShouldWriteNumbers()
        {
            this.writer.WriteValue(123);

            Assert.That(this.writer.Output, Is.EqualTo("123"));
        }

        [Test]
        public void WriteValueShouldWriteStrings()
        {
            this.writer.WriteValue(@"test\");

            Assert.That(this.writer.Output, Is.EqualTo(@"""test\\"""));
        }

        private class FakeJsonWriter : JsonWriter
        {
            private readonly StringWriter writer;

            public FakeJsonWriter()
                : this(new StringWriter())
            {
            }

            private FakeJsonWriter(StringWriter writer)
                : base(writer)
            {
                this.writer = writer;
            }

            internal string Output
            {
                get { return this.writer.ToString(); }
            }

            internal new void Write(char value)
            {
                base.Write(value);
            }

            internal new void Write(string value)
            {
                base.Write(value);
            }

            internal new void WriteRaw(string value)
            {
                base.WriteRaw(value);
            }

            internal new void WriteValue(object value)
            {
                base.WriteValue(value);
            }
        }
    }
}