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
                Action action = () => ((ISerializer<LinkCollection>)this.serializer).Read(null);

                action.Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Write : LinkCollectionSerializerTests
        {
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
        }
    }
}
