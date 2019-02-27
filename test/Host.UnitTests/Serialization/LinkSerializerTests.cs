namespace Host.UnitTests.Serialization
{
    using System;
    using Crest.Core;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class LinkSerializerTests
    {
        private readonly LinkSerializer serializer;
        private readonly IClassWriter writer;

        private LinkSerializerTests()
        {
            this.writer = Substitute.For<IClassWriter>();
            this.writer.Writer.Returns(Substitute.For<ValueWriter>());
            this.serializer = new LinkSerializer();
        }

        public sealed class Read : LinkSerializerTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                Action action = () => ((ICustomSerializer<Link>)this.serializer).Read(null);

                action.Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Write : LinkSerializerTests
        {
            [Fact]
            public void ShouldNotSerializeNameIfNull()
            {
                this.serializer.Write(this.writer, new Link(
                    null,
                    new Uri("http://www.example.com"),
                    null,
                    name: null,
                    null,
                    "relation",
                    false,
                    null,
                    null));

                this.writer.DidNotReceive().WriteBeginProperty(nameof(Link.Name));
                this.writer.Writer.DidNotReceive().WriteString(null);
            }

            [Fact]
            public void ShouldNotSerializeTemplatedIfFalse()
            {
                this.serializer.Write(this.writer, new Link(
                    null,
                    new Uri("http://www.example.com"),
                    null,
                    null,
                    null,
                    "relation",
                    templated: false,
                    null,
                    null));

                this.writer.DidNotReceive().WriteBeginProperty(nameof(Link.Templated));
                this.writer.Writer.DidNotReceive().WriteBoolean(false);
            }

            [Fact]
            public void ShouldSerializeNameIfNotNull()
            {
                this.serializer.Write(this.writer, new Link(
                    null,
                    new Uri("http://www.example.com"),
                    null,
                    "name value",
                    null,
                    "relation",
                    false,
                    null,
                    null));

                this.writer.Received().WriteBeginProperty(nameof(Link.Name));
                this.writer.Writer.Received().WriteString("name value");
            }

            [Fact]
            public void ShouldSerializeTemplatedIfTrue()
            {
                this.serializer.Write(this.writer, new Link(
                    null,
                    new Uri("http://www.example.com"),
                    null,
                    null,
                    null,
                    "relation",
                    templated: true,
                    null,
                    null));

                this.writer.Received().WriteBeginProperty(nameof(Link.Templated));
                this.writer.Writer.Received().WriteBoolean(true);
            }
        }
    }
}
