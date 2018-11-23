namespace Core.UnitTests
{
    using System;
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class LinkTests
    {
        public sealed class Constructor : LinkTests
        {
            [Fact]
            public void ShouldCheckForEmptyRelations()
            {
                new Action(() => new Link("", new Uri("http://www.example.com")))
                    .Should().Throw<ArgumentException>();
            }

            [Fact]
            public void ShouldCheckForNullArguments()
            {
                new Action(() => new Link(null, new Uri("http://www.example.com")))
                    .Should().Throw<ArgumentNullException>();

                new Action(() => new Link("relation", null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldSetTheProperties()
            {
                var uri = new Uri("http://www.example.com");

                var link = new Link("relation", uri);

                link.HRef.Should().BeSameAs(uri);
                link.RelationType.Should().Be("relation");
            }
        }

        public sealed class EqualsLink : LinkTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentReferences()
            {
                var link1 = new Link("r", new Uri("http://www.example.com/first"));
                var link2 = new Link("r", new Uri("http://www.example.com/second"));

                bool result = link1.Equals(link2);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheEqualReference()
            {
                var lower = new Link("r", new Uri("http://www.example.com"));
                var upper = new Link("r", new Uri("http://www.EXAMPLE.com"));

                bool result = lower.Equals(upper);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameReference()
            {
                var uri = new Uri("http://www.example.com");
                var link1 = new Link("r", uri);
                var link2 = new Link("r", uri);

                bool result = link1.Equals(link2);

                result.Should().BeTrue();
            }
        }

        public sealed class EqualsObject : LinkTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var uri = new Uri("http://www.example.com");
                var link = new Link("r", uri);

                bool result = link.Equals(uri.OriginalString);

                result.Should().BeFalse();
            }
        }

        public new sealed class GetHashCode : LinkTests
        {
            [Fact]
            public void ShouldReturnTheSameValueForTwoInstancesThatAreTheEqual()
            {
                var link1 = new Link("r", new Uri("http://www.example.com"));
                var link2 = new Link("r", new Uri("http://www.example.com"));

                int hashCode = link1.GetHashCode();

                hashCode.Should().Be(link2.GetHashCode());
            }
        }
    }
}
