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
            public void ShouldCheckForNullArguments()
            {
                Action action = () => new Link(null);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldImplicitlyConvertFromUri()
            {
                var uri = new Uri("http://www.example.com");

                Link link = uri;

                link.Reference.Should().BeSameAs(uri);
            }

            [Fact]
            public void ShouldSetTheProperties()
            {
                var uri = new Uri("http://www.example.com");

                var link = new Link(uri);

                link.Reference.Should().BeSameAs(uri);
            }
        }

        public sealed class EqualsLink : LinkTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentReferences()
            {
                var link1 = new Link(new Uri("http://www.example.com/first"));
                var link2 = new Link(new Uri("http://www.example.com/second"));

                bool result = link1.Equals(link2);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheEqualReference()
            {
                var lower = new Link(new Uri("http://www.example.com"));
                var upper = new Link(new Uri("http://www.EXAMPLE.com"));

                bool result = lower.Equals(upper);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameReference()
            {
                var uri = new Uri("http://www.example.com");
                var link1 = new Link(uri);
                var link2 = new Link(uri);

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
                var link = new Link(uri);

                bool result = link.Equals(uri.OriginalString);

                result.Should().BeFalse();
            }
        }

        public sealed class FromUri : LinkTests
        {
            [Fact]
            public void ShouldSetTheReferenceProperty()
            {
                var reference = new Uri("http://www.example.com");

                var result = Link.FromUri(reference);

                result.Reference.Should().BeSameAs(reference);
            }
        }

        public new sealed class GetHashCode : LinkTests
        {
            [Fact]
            public void ShouldReturnTheSameValueForTwoInstancesThatAreTheEqual()
            {
                var link1 = new Link(new Uri("http://www.example.com"));
                var link2 = new Link(new Uri("http://www.example.com"));

                int hashCode = link1.GetHashCode();

                hashCode.Should().Be(link2.GetHashCode());
            }
        }
    }
}
