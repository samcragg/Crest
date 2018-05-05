namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using Xunit;

    public class UrlParserTests
    {
        private readonly FakeUrlParser parser = new FakeUrlParser();

        public sealed class GetSegments : UrlParserTests
        {
            [Fact]
            public void ShouldReturnAllTheParts()
            {
                StringSegment[] segments = UrlParser.GetSegments("/one/two").ToArray();

                segments.Should().HaveCount(2);
                segments[0].ToString().Should().Be("one");
                segments[1].ToString().Should().Be("two");
            }
        }

        public sealed class ParseUrl : UrlParserTests
        {
            private static readonly ISet<string> NoOptional = new SortedSet<string>();

            [Fact]
            public void ShouldCaptureLiterals()
            {
                this.parser.ParseUrl("/literal/");

                this.parser.Literals.Single().Should().Be("literal");
            }

            [Fact]
            public void ShouldCaptureParameters()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "capture", typeof(int) }
                };

                this.parser.ParseUrl("/{capture}/", parameters, NoOptional);

                this.parser.Captures.Single().type.Should().Be(typeof(int));
                this.parser.Captures.Single().name.Should().Be("capture");
            }

            [Fact]
            public void ShouldCaptureQueryParemters()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "query1", typeof(int) },
                    { "query2", typeof(int) }
                };

                this.parser.ParseUrl(
                    "/literal?key1={query1}&key2={query2}",
                    parameters,
                    new SortedSet<string>(new[] { "query1", "query2" }));

                this.parser.QueryParameters.Keys
                    .Should().BeEquivalentTo("key1", "key2");

                this.parser.QueryParameters.Values.Select(x => x.name)
                    .Should().BeEquivalentTo("query1", "query2");
            }

            [Fact]
            public void ShouldCheckForDuplicateParameters()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "parameter", typeof(int) }
                };

                this.parser.ParseUrl("/{parameter}/{parameter}", parameters, NoOptional);

                this.parser.ErrorParameters.Single().Should().Be("parameter");
            }

            [Fact]
            public void ShouldCheckForMissingClosingBraces()
            {
                this.parser.ParseUrl("/{234/");

                this.parser.ErrorParts.Single().Should().Be("4");
            }

            [Fact]
            public void ShouldCheckForMissingQueryValueCaptures()
            {
                this.parser.ParseUrl("/literal?key");

                this.parser.ErrorParts.Single().Should().Be("key");
            }

            [Fact]
            public void ShouldCheckForUnescapedBraces()
            {
                this.parser.ParseUrl("/123}/");
                this.parser.ParseUrl("/123{/");

                this.parser.ErrorParts.Should().BeEquivalentTo("}", "{");
            }

            [Fact]
            public void ShouldCheckForUnmatchedParameters()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "parameter", typeof(int) }
                };

                this.parser.ParseUrl("/literal", parameters, NoOptional);

                this.parser.ErrorParameters.Single().Should().Be("parameter");
            }

            [Fact]
            public void ShouldCheckForValidParameters()
            {
                this.parser.ParseUrl("/{missingParameter}/");

                this.parser.ErrorParts.Single().Should().Be("missingParameter");
            }

            [Fact]
            public void ShouldCheckQueryCapturesSyntax()
            {
                this.parser.ParseUrl("/literal?key={missingClosingBrace");

                this.parser.ErrorParts.Single().Should().Be("e");
            }

            [Fact]
            public void ShouldCheckQueryParemtersAreOptional()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "query", typeof(int) }
                };

                this.parser.ParseUrl("/literal?key={query}", parameters, NoOptional);

                this.parser.QueryParameters.Should().BeEmpty();
                this.parser.ErrorParameters.Single().Should().Be("query");
            }

            [Fact]
            public void ShouldCheckQueryValuesAreCaptures()
            {
                this.parser.ParseUrl("/literal?key=value");

                this.parser.ErrorParts.Single().Should().Be("value");
            }

            [Fact]
            public void ShouldUnescapeBraces()
            {
                this.parser.ParseUrl("/{{escaped_braces}}/");

                this.parser.Literals.Single().Should().Be("{escaped_braces}");
            }
        }

        private class FakeUrlParser : UrlParser
        {
            private string routeUrl;

            internal List<(Type type, string name)> Captures { get; } = new List<(Type type, string name)>();
            internal List<string> ErrorParameters { get; } = new List<string>();
            internal List<string> ErrorParts { get; } = new List<string>();
            internal List<string> Literals { get; } = new List<string>();
            internal Dictionary<string, (Type type, string name)> QueryParameters { get; } = new Dictionary<string, (Type type, string name)>();

            internal void ParseUrl(string routeUrl)
            {
                this.ParseUrl(routeUrl, new Dictionary<string, Type>(), new HashSet<string>());
            }

            internal override void ParseUrl(string routeUrl, IReadOnlyDictionary<string, Type> parameters, ISet<string> optionalParameters)
            {
                this.routeUrl = routeUrl;
                base.ParseUrl(routeUrl, parameters, optionalParameters);
            }

            protected override void OnCaptureSegment(Type parameterType, string name)
            {
                this.Captures.Add((parameterType, name));
            }

            protected override void OnError(ErrorType error, string parameter)
            {
                this.ErrorParameters.Add(parameter);
            }

            protected override void OnError(ErrorType error, int start, int length, string value)
            {
                this.ErrorParts.Add(this.routeUrl.Substring(start, length));
            }

            protected override void OnLiteralSegment(string value)
            {
                this.Literals.Add(value);
            }

            protected override void OnQueryParameter(string key, Type parameterType, string name)
            {
                this.QueryParameters[key] = (parameterType, name);
            }
        }
    }
}
