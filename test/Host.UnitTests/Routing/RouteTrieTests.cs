namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using FluentAssertions;
    using Xunit;

    public class RouteTrieTests
    {
        public sealed class Match : RouteTrieTests
        {
            [Fact]
            public void ShouldReturnNoMatchIfNotAFullMatch()
            {
                RouteTrie<string> root = CreateNode("root-", "", CreateNode("child", "child"));

                RouteTrie<string>.MatchResult result = root.Match("root-chil".AsSpan());

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnMatchIfCaseIsDifferent()
            {
                RouteTrie<string> node = CreateNode("root", "value");

                RouteTrie<string>.MatchResult result = node.Match("ROOT".AsSpan());

                result.Success.Should().BeTrue();
                result.Values.Should().ContainSingle().Which.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnTheCaptureValues()
            {
                var captureNode = new RouteTrie<string>(
                    new CaptureNode("capture", 123),
                    new[] { "node-value" },
                    Array.Empty<RouteTrie<string>>());
                RouteTrie<string> root = CreateNode("root-", "", captureNode);

                RouteTrie<string>.MatchResult result = root.Match("root-capture".AsSpan());

                result.Success.Should().BeTrue();
                result.Values.Should().ContainSingle().Which.Should().Be("node-value");

                KeyValuePair<string, object> capture = result.Captures.Should().ContainSingle().Subject;
                capture.Key.Should().Be(CaptureNode.CaptureKey);
                capture.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReturnTheValueAssociatedWithTheFullMatch()
            {
                RouteTrie<string> child1 = CreateNode("child1", "child1");
                RouteTrie<string> child2 = CreateNode("child2", "child2");
                RouteTrie<string> root = CreateNode("root-", "root", child1, child2);

                RouteTrie<string>.MatchResult result = root.Match("root-child2".AsSpan());

                result.Success.Should().BeTrue();
                result.Values.Should().ContainSingle().Which.Should().Be("child2");
                result.Captures.Should().BeEmpty();
            }

            private static RouteTrie<string> CreateNode(string match, string value, params RouteTrie<string>[] children)
            {
                return new RouteTrie<string>(
                    new LiteralNode(match),
                    new[] { value },
                    children);
            }

            private class CaptureNode : IMatchNode
            {
                internal const string CaptureKey = nameof(CaptureNode);
                private readonly string match;
                private readonly int value;

                public CaptureNode(string match, int value)
                {
                    this.match = match;
                    this.value = value;
                }

                public int Priority { get; }
                public string ParameterName { get; }

                public bool Equals(IMatchNode other)
                {
                    return false;
                }

                public NodeMatchInfo Match(ReadOnlySpan<char> text)
                {
                    if (text.StartsWith(this.match.AsSpan()))
                    {
                        return new NodeMatchInfo(this.match.Length, CaptureKey, this.value);
                    }
                    else
                    {
                        return NodeMatchInfo.None;
                    }
                }

                public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
                {
                    result = null;
                    return false;
                }
            }
        }
    }
}
