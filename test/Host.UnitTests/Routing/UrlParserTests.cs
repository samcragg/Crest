namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                const string Url = "/one/two";
                (int start, int length)[] segments = UrlParser.GetSegments(Url);

                segments.Should().HaveCount(2);
                Url.Substring(segments[0].start, segments[0].length).Should().Be("one");
                Url.Substring(segments[1].start, segments[1].length).Should().Be("two");
            }
        }

        public sealed class ParseUrl : UrlParserTests
        {
            [Fact]
            public void ShouldAllowImplicitBodyParameters()
            {
                this.parser.ParseUrl("/", new FakeParameter("body"));

                (Type type, string name) = this.parser.Body.Should().ContainSingle().Subject;
                type.Should().Be(typeof(int));
                name.Should().Be("body");
            }

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

                (Type type, string name) = this.parser.Captures.Should().ContainSingle().Subject;
                type.Should().Be(typeof(int));
                name.Should().Be("capture");
            }

            [Fact]
            public void ShouldCaptureQueryCatchAllParameter()
            {
                this.parser.ParseUrl(
                    "/literal?*={all}",
                    new FakeParameter("all") { Type = typeof(object) });

                this.parser.CatchAll.Should().Be("all");
            }

            [Fact]
            public void ShouldCaptureQueryParameters()
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
            public void ShouldCheckCapturesAreNotMarkedAsFromBody()
            {
                this.parser.ParseUrl(
                    "/{parameter}/",
                    new FakeParameter("parameter") { HasBodyAttribute = true });

                this.parser.ErrorParameters.Should().ContainSingle("parameter");
            }

            [Fact]
            public void ShouldCheckCatchAllType()
            {
                this.parser.ParseUrl(
                    "/literal?*={invalid}",
                    new FakeParameter("invalid") { Type = typeof(string) });

                this.parser.ErrorParameters.Should().ContainSingle("all");
            }

            [Fact]
            public void ShouldCheckForDuplicateParameters()
            {
                this.parser.ParseUrl("/{parameter}/{parameter}", new FakeParameter("parameter"));

                this.parser.ErrorParameters.Should().ContainSingle("parameter");
            }

            [Fact]
            public void ShouldCheckForMissingClosingBraces()
            {
                this.parser.ParseUrl("/{234/");

                this.parser.ErrorParts.Should().ContainSingle("4");
            }

            [Fact]
            public void ShouldCheckForMissingQueryValueCaptures()
            {
                this.parser.ParseUrl("/literal?key");

                this.parser.ErrorParts.Should().ContainSingle("key");
            }

            [Fact]
            public void ShouldCheckForMultipleBodyParameters()
            {
                this.parser.ParseUrl(
                    "/",
                    new FakeParameter("parameter1") { HasBodyAttribute = true },
                    new FakeParameter("parameter2") { HasBodyAttribute = true });

                this.parser.ErrorParameters.Should().ContainSingle("parameter2");
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
                var noBodyParametersParser = new FakeUrlParser(canReadBody: false);

                noBodyParametersParser.ParseUrl("/literal", new FakeParameter("parameter"));

                noBodyParametersParser.ErrorParameters.Should().ContainSingle("parameter");
            }

            [Fact]
            public void ShouldCheckForValidParameters()
            {
                this.parser.ParseUrl("/{missingParameter}/");

                this.parser.ErrorParts.Should().ContainSingle("missingParameter");
            }

            [Fact]
            public void ShouldCheckQueryCapturesSyntax()
            {
                this.parser.ParseUrl("/literal?key={missingClosingBrace");

                this.parser.ErrorParts.Should().ContainSingle("e");
            }

            [Fact]
            public void ShouldCheckQueryParemtersAreOptional()
            {
                this.parser.ParseUrl(
                    "/literal?key={query}",
                    new FakeParameter("query") { IsOptional = false });

                this.parser.QueryParameters.Should().BeEmpty();
                this.parser.ErrorParameters.Should().ContainSingle("query");
            }

            [Fact]
            public void ShouldCheckQueryValuesAreCaptures()
            {
                this.parser.ParseUrl("/literal?key=value");

                this.parser.ErrorParts.Should().ContainSingle("value");
            }

            [Fact]
            public void ShouldUnescapeBraces()
            {
                this.parser.ParseUrl("/{{escaped_braces}}/");

                this.parser.Literals.Should().ContainSingle("{escaped_braces}");
            }
        }

        private class FakeParameter
        {
            public FakeParameter(string name)
            {
                this.Name = name;
            }

            public bool HasBodyAttribute { get; set; }

            public bool IsOptional { get; set; }

            public string Name { get; }

            public Type Type { get; set; } = typeof(int);
        }

        private class FakeUrlParser : UrlParser
        {
            private string routeUrl;

            public FakeUrlParser(bool canReadBody = true) : base(canReadBody)
            {
            }

            internal List<(Type type, string name)> Body { get; } = new List<(Type type, string name)>();
            internal List<(Type type, string name)> Captures { get; } = new List<(Type type, string name)>();
            internal string CatchAll { get; private set; }
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
                        HasBodyAttribute = p.HasBodyAttribute,
                        IsOptional = p.IsOptional,
                        Name = p.Name,
                        ParameterType = p.Type
                    }));
            }

            protected override void OnCaptureBody(Type parameterType, string name)
            {
                this.Body.Add((parameterType, name));
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

            protected override void OnQueryCatchAll(string name)
            {
                this.CatchAll = name;
            }

            protected override void OnQueryParameter(string key, Type parameterType, string name)
            {
                this.QueryParameters[key] = (parameterType, name);
            }
        }
    }
}
