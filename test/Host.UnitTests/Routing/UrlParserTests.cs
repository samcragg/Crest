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
            [Fact]
            public void ShouldCaptureLiterals()
            {
                this.parser.ParseUrl("/literal/");

                this.parser.Literals.Single().Should().Be("literal");
            }

            [Fact]
            public void ShouldCaptureParameters()
            {
                this.parser.ParseUrl("/{capture}/", new FakeParameter("capture"));

                this.parser.Captures.Single().type.Should().Be(typeof(int));
                this.parser.Captures.Single().name.Should().Be("capture");
            }

            [Fact]
            public void ShouldCaptureQueryParemters()
            {
                this.parser.ParseUrl(
                    "/literal?key1={query1}&key2={query2}",
                    new FakeParameter("query1") { IsOptional = true },
                    new FakeParameter("query2") { IsOptional = true });

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

                this.parser.ParseUrl("/{parameter}/{parameter}", new FakeParameter("parameter"));

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
                this.parser.ParseUrl("/literal", new FakeParameter("parameter"));

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
                this.parser.ParseUrl(
                    "/literal?key={query}",
                    new FakeParameter("query") { IsOptional = false });

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

        private class FakeParameter
        {
            public FakeParameter(string name)
            {
                this.Name = name;
            }

            public bool IsOptional { get; set; }

            public string Name { get; }
        }

        private class FakeUrlParser : UrlParser
        {
            private string routeUrl;

            internal List<(Type type, string name)> Captures { get; } = new List<(Type type, string name)>();
            internal List<string> ErrorParameters { get; } = new List<string>();
            internal List<string> ErrorParts { get; } = new List<string>();
            internal List<string> Literals { get; } = new List<string>();
            internal Dictionary<string, (Type type, string name)> QueryParameters { get; } = new Dictionary<string, (Type type, string name)>();

            internal void ParseUrl(string routeUrl, params FakeParameter[] parameters)
            {
                this.routeUrl = routeUrl;
                base.ParseUrl(
                    routeUrl,
                    parameters.Select(p => new ParameterData
                    {
                        IsOptional = p.IsOptional,
                        Name = p.Name,
                        ParameterType = typeof(int)
                    }).ToDictionary(pd => pd.Name));
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
