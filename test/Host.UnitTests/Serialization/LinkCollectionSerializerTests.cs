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
        private static readonly Uri ExampleUri = new Uri("http://www.example.com");
        private readonly ISerializer<Link> linkSerializer;
        private readonly LinkCollectionSerializer serializer;
        private readonly IClassWriter writer;

        private LinkCollectionSerializerTests()
        {
            this.linkSerializer = Substitute.For<ISerializer<Link>>();
            this.writer = Substitute.For<IClassWriter>();
            this.writer.Writer.Returns(Substitute.For<ValueWriter>());
            this.serializer = new LinkCollectionSerializer(this.linkSerializer);
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
                var link1 = new Link("multiple", ExampleUri);
                var link2 = new Link("multiple", ExampleUri);
                this.serializer.Write(this.writer, new LinkCollection { link1, link2 });

                this.writer.Received().WriteBeginProperty("multiple");
                this.writer.Received().WriteBeginArray(typeof(Link), 2);
                this.linkSerializer.Received().Write(this.writer, link1);
                this.linkSerializer.Received().Write(this.writer, link2);
            }

            [Fact]
            public void ShouldSerializeSingleLinks()
            {
                var link = new Link("single", ExampleUri);
                this.serializer.Write(this.writer, new LinkCollection { link });

                this.writer.DidNotReceiveWithAnyArgs().WriteBeginArray(null, 0);

                this.writer.Received().WriteBeginProperty("single");
                this.linkSerializer.Received().Write(this.writer, link);
            }
        }
    }
}
