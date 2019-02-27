namespace Host.UnitTests.Serialization
{
    using System;
    using Crest.Core;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class LinkCollectionSerializerTests
    {
        private readonly LinkCollectionSerializer serializer;
        private readonly IClassWriter writer;

        private LinkCollectionSerializerTests()
        {
            this.writer = Substitute.For<IClassWriter>();
            this.writer.Writer.Returns(Substitute.For<ValueWriter>());
            this.serializer = new LinkCollectionSerializer();
        }

        public sealed class Read : LinkCollectionSerializerTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                Action action = () => ((ICustomSerializer<LinkCollection>)this.serializer).Read(null);

                action.Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Write : LinkCollectionSerializerTests
        {
            [Fact]
            public void ShouldNotSerializeNameIfNull()
            {
                this.serializer.Write(this.writer, new LinkCollection
                {
                    new Link(null, new Uri("http://www.example.com"), null, name: null, null, "relation", false, null, null)
                });

                this.writer.DidNotReceive().WriteBeginProperty(nameof(Link.Name));
                this.writer.Writer.DidNotReceive().WriteString(null);
            }

            [Fact]
            public void ShouldNotSerializeTemplatedIfFalse()
            {
                this.serializer.Write(this.writer, new LinkCollection
                {
                    new Link(null, new Uri("http://www.example.com"), null, null, null, "relation", templated: false, null, null)
                });

                this.writer.DidNotReceive().WriteBeginProperty(nameof(Link.Templated));
                this.writer.Writer.DidNotReceive().WriteBoolean(false);
            }

            [Fact]
            public void ShouldSerializeMultipleLinks()
            {
                var route1 = new Uri("http://www.example.com/route1");
                var route2 = new Uri("http://www.example.com/route2");
                this.serializer.Write(this.writer, new LinkCollection
                {
                    { "multiple", route1 },
                    { "multiple", route2 },
                });

                this.writer.Received().WriteBeginProperty("multiple");
                this.writer.Received().WriteBeginArray(typeof(Link), 2);
                this.writer.Received(2).WriteBeginProperty(nameof(Link.HRef));
                this.writer.Writer.Received().WriteUri(route1);
                this.writer.Writer.Received().WriteUri(route2);
            }

            [Fact]
            public void ShouldSerializeNameIfNotNull()
            {
                this.serializer.Write(this.writer, new LinkCollection
                {
                    new Link(null, new Uri("http://www.example.com"), null, "name value", null, "relation", false, null, null)
                });

                this.writer.Received().WriteBeginProperty(nameof(Link.Name));
                this.writer.Writer.Received().WriteString("name value");
            }

            [Fact]
            public void ShouldSerializeSingleLinks()
            {
                var uri = new Uri("http://www.example.com");
                this.serializer.Write(this.writer, new LinkCollection
                {
                    { "single", uri }
                });

                this.writer.DidNotReceiveWithAnyArgs().WriteBeginArray(null, 0);
                this.writer.Received().WriteBeginProperty("single");
                this.writer.Received().WriteBeginProperty(nameof(Link.HRef));
                this.writer.Writer.Received().WriteUri(uri);
            }

            [Fact]
            public void ShouldSerializeTemplatedIfTrue()
            {
                this.serializer.Write(this.writer, new LinkCollection
                {
                    new Link(null, new Uri("http://www.example.com"), null, null, null, "relation", templated: true, null, null)
                });

                this.writer.Received().WriteBeginProperty(nameof(Link.Templated));
                this.writer.Writer.Received().WriteBoolean(true);
            }
        }
    }
}
