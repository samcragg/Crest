namespace Core.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class LinkCollectionTests
    {
        private readonly LinkCollection collection = new LinkCollection();

        private static Link CreateLink(string path = "")
        {
            return new Link(new Uri("http://www.example.com/" + path));
        }

        public sealed class Add : LinkCollectionTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                this.collection.Invoking(x => x.Add(null, CreateLink()))
                    .Should().Throw<ArgumentNullException>();

                this.collection.Invoking(x => x.Add("self", null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowForDuplicateRelations()
            {
                this.collection.Add("relation", CreateLink());

                Action action = () => this.collection.Add("relation", CreateLink("other"));

                action.Should().Throw<ArgumentException>()
                      .WithMessage("*already exists*");
            }
        }

        public sealed class AddLink : LinkCollectionTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                Action action = () => ((ICollection<Link>)this.collection).Add(CreateLink());

                action.Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Clear : LinkCollectionTests
        {
            [Fact]
            public void ShouldRemoveTheItems()
            {
                this.collection.Add("1", CreateLink("1"));
                this.collection.Add("2", CreateLink("2"));

                this.collection.Clear();

                this.collection.Count.Should().Be(0);
                this.collection.Should().BeEmpty();
            }
        }

        public sealed class Contains : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheLinkDoesNotExist()
            {
                this.collection.Add("relation", CreateLink("1"));

                bool result = this.collection.Contains(CreateLink("2"));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheLinkExists()
            {
                this.collection.Add("relation", CreateLink("1"));

                bool result = this.collection.Contains(CreateLink("1"));

                result.Should().BeTrue();
            }
        }

        public sealed class ContainsKey : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheLinkDoesNotExist()
            {
                this.collection.Add("relation", CreateLink("link"));

                bool result = this.collection.ContainsKey("link");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheLinkExists()
            {
                this.collection.Add("relation", CreateLink("link"));

                bool result = this.collection.ContainsKey("relation");

                result.Should().BeTrue();
            }
        }

        public sealed class CopyTo : LinkCollectionTests
        {
            [Fact]
            public void ShouldCheckForNegativeArrayIndexes()
            {
                Action action = () => this.collection.CopyTo(new Link[1], -1);

                action.Should().Throw<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => this.collection.CopyTo(null, 0);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldCheckTheDestinationIsBigEnough()
            {
                this.collection.Add("1", CreateLink("1"));

                Action action = () => this.collection.CopyTo(new Link[1], 1);

                action.Should().Throw<ArgumentException>()
                      .WithMessage("*long enough*");
            }

            [Fact]
            public void ShouldCopyTheElements()
            {
                var destination = new Link[2];
                Link link = CreateLink();
                this.collection.Add("relation", link);

                this.collection.CopyTo(destination, 1);

                destination.Should().HaveElementAt(0, null);
                destination.Should().HaveElementAt(1, link);
            }
        }

        public sealed class GetEnumerator : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnAllTheItems()
            {
                this.collection.Add("1", CreateLink());
                this.collection.Add("2", CreateLink());

                IEnumerator<Link> result = this.collection.GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeFalse();
            }
        }

        public sealed class IsReadOnly : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                bool result = ((ICollection<Link>)this.collection).IsReadOnly;

                result.Should().BeFalse();
            }
        }

        public sealed class NonGenericGetEnumerator : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnAllTheItems()
            {
                this.collection.Add("1", CreateLink());
                this.collection.Add("2", CreateLink());

                IEnumerator result = ((IEnumerable)this.collection).GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeFalse();
            }
        }

        public sealed class RemoveLink : LinkCollectionTests
        {
            [Fact]
            public void ShouldRemoveTheLink()
            {
                this.collection.Add("1", CreateLink("1"));
                this.collection.Add("2", CreateLink("2"));

                bool result = this.collection.Remove(CreateLink("2"));

                result.Should().BeTrue();
                this.collection.Should().ContainSingle()
                    .Which.Should().Be(CreateLink("1"));
            }

            [Fact]
            public void ShouldReturnFalseIfTheLinkIsNotFound()
            {
                this.collection.Add("1", CreateLink("1"));

                bool result = this.collection.Remove(CreateLink("2"));

                result.Should().BeFalse();
            }
        }

        public sealed class RemoveString : LinkCollectionTests
        {
            [Fact]
            public void ShouldRemoveTheRelation()
            {
                this.collection.Add("relation1", CreateLink("1"));
                this.collection.Add("relation2", CreateLink("2"));

                bool result = this.collection.Remove("relation2");

                result.Should().BeTrue();
                this.collection.Should().ContainSingle()
                    .Which.Should().Be(CreateLink("1"));
            }

            [Fact]
            public void ShouldReturnFalseIfTheRelationIsNotFound()
            {
                this.collection.Add("1", CreateLink());

                bool result = this.collection.Remove("2");

                result.Should().BeFalse();
            }
        }
    }
}
