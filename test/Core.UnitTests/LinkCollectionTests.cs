namespace Core.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class LinkCollectionTests
    {
        private static readonly Uri ValidUri = new Uri("http://www.example.com/");
        private readonly LinkCollection collection = new LinkCollection();

        private static Link CreateLink(string relation, string path = "")
        {
            return new Link(relation, new Uri(ValidUri, path));
        }

        public sealed class Add : LinkCollectionTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                this.collection.Invoking(x => x.Add(null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldMaintinOrderForLinksWithTheSameRelation()
            {
                Link link1 = CreateLink("sameRelation", "insertedFirst");
                Link link2 = CreateLink("sameRelation", "insertedAfter");

                this.collection.Add(link1);
                this.collection.Add(link2);
                var links = new List<Link>(this.collection);

                links.Should().ContainInOrder(link1, link2);
            }
        }

        public sealed class AddRelationAndUri : LinkCollectionTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                this.collection.Invoking(x => x.Add(null, ValidUri))
                    .Should().Throw<ArgumentNullException>();

                this.collection.Invoking(x => x.Add("self", null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldAddANewLink()
            {
                this.collection.Add("relation", ValidUri);

                this.collection["relation"].Single().HRef.Should().Be(ValidUri);
            }
        }

        public sealed class Clear : LinkCollectionTests
        {
            [Fact]
            public void ShouldRemoveTheItems()
            {
                this.collection.Add(CreateLink("1"));
                this.collection.Add(CreateLink("2"));

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
                this.collection.Add(CreateLink("relation", "1"));

                bool result = this.collection.Contains(CreateLink("relation", "2"));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheLinkExists()
            {
                this.collection.Add(CreateLink("relation", "1"));

                bool result = this.collection.Contains(CreateLink("relation", "1"));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTrueIfTheRelationExists()
            {
                this.collection.Add(CreateLink("relation", "link"));

                bool result = ((ILookup<string, Link>)this.collection).Contains("relation");

                result.Should().BeTrue();
            }
        }

        public sealed class ContainsKey : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheRelationDoesNotExist()
            {
                this.collection.Add(CreateLink("relation", "link"));

                bool result = this.collection.ContainsKey("link");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheRelationExists()
            {
                this.collection.Add(CreateLink("relation", "link"));

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
                this.collection.Add(CreateLink("1"));

                Action action = () => this.collection.CopyTo(new Link[1], 1);

                action.Should().Throw<ArgumentException>()
                      .WithMessage("*long enough*");
            }

            [Fact]
            public void ShouldCopyTheElements()
            {
                var destination = new Link[2];
                Link link = CreateLink("1");
                this.collection.Add(link);

                this.collection.CopyTo(destination, 1);

                destination.Should().HaveElementAt(0, null);
                destination.Should().HaveElementAt(1, link);
            }
        }

        public sealed class GetEnumerator : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnAllTheLinks()
            {
                this.collection.Add(CreateLink("1"));
                this.collection.Add(CreateLink("2"));

                IEnumerator<Link> result = this.collection.GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnAllTheGroups()
            {
                this.collection.Add(CreateLink("rel1", "A"));
                this.collection.Add(CreateLink("rel2", "B"));
                this.collection.Add(CreateLink("rel1", "C"));

                IEnumerator<IGrouping<string, Link>> result =
                    ((ILookup<string, Link>)this.collection).GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.Current.Key.Should().Be("rel1");
                ((IReadOnlyCollection<Link>)result.Current).Count.Should().Be(2);

                result.MoveNext().Should().BeTrue();
                result.Current.Key.Should().Be("rel2");
                ((IReadOnlyCollection<Link>)result.Current).Count.Should().Be(1);

                result.MoveNext().Should().BeFalse();
            }
        }

        public sealed class Index : LinkCollectionTests
        {
            [Fact]
            public void ShouldReturnAllTheLinksWithTheSameRelation()
            {
                this.collection.Add(CreateLink("rel1", "A"));
                this.collection.Add(CreateLink("rel2", "B"));
                this.collection.Add(CreateLink("rel1", "C"));

                IEnumerable<Link> result = this.collection["rel1"];

                result.Select(l => l.HRef.PathAndQuery)
                      .Should().BeEquivalentTo("/A", "/C");
            }

            [Fact]
            public void ShouldReturnAnEmptyCollecitonIfTheKeyIsNotFound()
            {
                IEnumerable<Link> result = this.collection["unknown"];

                result.Should().BeEmpty();
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
                this.collection.Add(CreateLink("1"));
                this.collection.Add(CreateLink("2"));

                IEnumerator result = ((IEnumerable)this.collection).GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnAllTheItemsInTheGroup()
            {
                this.collection.Add(CreateLink("a"));
                this.collection.Add(CreateLink("a"));

                IEnumerator result = ((IEnumerable)this.collection["a"]).GetEnumerator();

                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeTrue();
                result.MoveNext().Should().BeFalse();
            }
        }

        public sealed class Remove : LinkCollectionTests
        {
            [Fact]
            public void ShouldRemoveTheLink()
            {
                this.collection.Add(CreateLink("A", "1"));
                this.collection.Add(CreateLink("B", "2"));

                bool result = this.collection.Remove(CreateLink("A", "1"));

                result.Should().BeTrue();
                ((IEnumerable<Link>)this.collection).Should().ContainSingle()
                    .Which.Should().Be(CreateLink("B", "2"));
            }

            [Fact]
            public void ShouldReturnFalseIfTheLinkIsNotFound()
            {
                this.collection.Add(CreateLink("A", "1"));

                bool result = this.collection.Remove(CreateLink("B", "2"));

                result.Should().BeFalse();
            }
        }
    }
}
