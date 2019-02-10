namespace Host.UnitTests.Routing.Parsing
{
    using System;
    using System.Text;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using Crest.Host.Routing.Parsing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteTrieBuilderTests
    {
        private readonly RouteTrieBuilder<string> builder = new RouteTrieBuilder<string>();

        public sealed class Add : RouteTrieBuilderTests
        {
            [Fact]
            public void ShouldCheckForAmbiguousUrlValues()
            {
                this.builder.Add(new[] { new LiteralNode("abc") }, "123");

                Action action = () => this.builder.Add(new[] { new LiteralNode("abc") }, "123");

                action.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class Build : RouteTrieBuilderTests
        {
            [Fact]
            public void ShouldCreateNodesForValues()
            {
                this.builder.Add(new[] { new LiteralNode("abc") }, "1");
                this.builder.Add(new[] { new LiteralNode("ab") }, "2");

                string tree = GetTreeAsString(this.builder.Build());

                tree.Should().Be(
                    "ab[2]\n" +
                    "  c[1]\n");
            }

            [Fact]
            public void ShouldMergeMatchers()
            {
                IMatchNode node = Substitute.For<IMatchNode>();
                node.Equals(Arg.Any<IMatchNode>()).Returns(true);

                this.builder.Add(new[] { node, new LiteralNode("a") }, "123");
                this.builder.Add(new[] { node, new LiteralNode("b") }, "456");

                string tree = GetTreeAsString(this.builder.Build());

                tree.Should().Be(
                    node.ToString() + "\n" +
                    "  a[123]\n" +
                    "  b[456]\n");
            }

            [Fact]
            public void ShouldMixLiteralsAndMatchers()
            {
                IMatchNode node = Substitute.For<IMatchNode>();
                this.builder.Add(new IMatchNode[] { new LiteralNode("abc"), node, new LiteralNode("def") }, "1");

                string tree = GetTreeAsString(this.builder.Build());

                tree.Should().Be(
                    "abc\n" +
                    "  " + node.ToString() + "\n" +
                    "    def[1]\n");
            }

            [Fact]
            public void ShouldSplitLiteralsWithCommonPrefix()
            {
                this.builder.Add(new[] { new LiteralNode("abc-one") }, "1");
                this.builder.Add(new[] { new LiteralNode("abc-two") }, "2");

                string tree = GetTreeAsString(this.builder.Build());

                tree.Should().Be(
                    "abc-\n" +
                    "  one[1]\n" +
                    "  two[2]\n");
            }

            private static string GetMatcherString(IMatchNode node)
            {
                if (node is LiteralNode literal)
                {
                    return literal.Literal;
                }
                else
                {
                    return node.ToString();
                }
            }

            private static string GetTreeAsString(RouteTrie<string> trie)
            {
                var builder = new StringBuilder();
                trie.VisitNodes((depth, matcher, values) =>
                {
                    // The first node is used to house all the actual parsed
                    // nodes so skip it
                    int indent = (depth - 1) * 2;
                    if (indent >= 0)
                    {
                        builder.Append(' ', indent).Append(GetMatcherString(matcher));
                        if (values.Length > 0)
                        {
                            builder.AppendFormat("[{0}]", string.Join(',', values));
                        }
                        builder.Append('\n');
                    }
                });

                return builder.ToString();
            }
        }
    }
}
