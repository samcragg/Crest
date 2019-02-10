namespace Host.UnitTests.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Routing.Parsing;
    using FluentAssertions;
    using Xunit;

    public class UrlParserTests
    {
        private readonly FakeUrlParser parser = new FakeUrlParser();

        public sealed class ParseUrl : UrlParserTests
        {
            [Fact]
            public void ShouldAllowExplicitBodyParameters()
            {
                this.parser.ParseUrl(
                    "/{query}",
                    new FakeParameter("query"),
                    new FakeParameter("body") { HasBodyAttribute = true });

                (Type type, string name) = this.parser.Body.Should().ContainSingle().Subject;
                type.Should().Be(typeof(int));
                name.Should().Be("body");
            }

            [Fact]
            public void ShouldAllowImplicitBodyParameters()
            {
                this.parser.ParseUrl("/", new FakeParameter("body") { HasBodyAttribute = false });

                (Type type, string name) = this.parser.Body.Should().ContainSingle().Subject;
                type.Should().Be(typeof(int));
                name.Should().Be("body");
            }

            [Fact]
            public void ShouldCaptureLiterals()
            {
                this.parser.ParseUrl("/literal/");

                this.parser.Literals.Single().Should().Be("/literal/");
            }

            [Fact]
            public void ShouldCaptureOptionalQueryParameters()
            {
                this.parser.ParseUrl(
                    "/literal{?query1,query2}",
                    new FakeParameter("query1") { IsOptional = true },
                    new FakeParameter("query2") { Type = typeof(string) });

                this.parser.QueryParameters.Keys
                    .Should().BeEquivalentTo("query1", "query2");
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
                    "/literal{?all*}",
                    new FakeParameter("all") { Type = typeof(object) });

                this.parser.CatchAll.Should().Be("all");
            }

            [Fact]
            public void ShouldCheckCapturesAreNotMarkedAsFromBody()
            {
                this.parser.ParseUrl(
                    "/{parameter}/",
                    new FakeParameter("parameter") { HasBodyAttribute = true });

                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("parameter");
            }

            [Fact]
            public void ShouldCheckCatchAllType()
            {
                this.parser.ParseUrl(
                    "/literal{?invalid*}",
                    new FakeParameter("invalid") { Type = typeof(string) });

                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("invalid");
            }

            [Fact]
            public void ShouldCheckForCatchAllParameterExists()
            {
                this.parser.ParseUrl("/{?missingParameter*}");

                this.parser.ErrorParts.Should().ContainSingle()
                    .Which.Should().Be("missingParameter");
            }

            [Fact]
            public void ShouldCheckForDuplicateParameters()
            {
                this.parser.ParseUrl("/{parameter}/{parameter}", new FakeParameter("parameter"));

                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("parameter");
            }

            [Fact]
            public void ShouldCheckForMissingClosingBraces()
            {
                this.parser.ParseUrl("/{234/");

                this.parser.ErrorParts.Should().ContainSingle()
                    .Which.Should().Be("{");
            }

            [Fact]
            public void ShouldCheckForMultipleBodyParameters()
            {
                this.parser.ParseUrl(
                    "/",
                    new FakeParameter("parameter1") { HasBodyAttribute = true },
                    new FakeParameter("parameter2") { HasBodyAttribute = true });

                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("parameter2");
            }

            [Fact]
            public void ShouldCheckForMultipleCatchAllParameters()
            {
                this.parser.ParseUrl(
                    "/{?catch1*,catch2*}",
                    new FakeParameter("catch1") { Type = typeof(object) },
                    new FakeParameter("catch2") { Type = typeof(object) });

                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("catch2");
            }

            [Fact]
            public void ShouldCheckForUnmatchedParameters()
            {
                var noBodyParametersParser = new FakeUrlParser(canReadBody: false);

                noBodyParametersParser.ParseUrl("/literal", new FakeParameter("parameter"));

                noBodyParametersParser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("parameter");
            }

            [Fact]
            public void ShouldCheckParameterExists()
            {
                this.parser.ParseUrl("/{missingParameter}/");

                this.parser.ErrorParts.Should().ContainSingle()
                    .Which.Should().Be("missingParameter");
            }

            [Fact]
            public void ShouldCheckQueryParameterExists()
            {
                this.parser.ParseUrl("/{?missingParameter}");

                this.parser.ErrorParts.Should().ContainSingle()
                    .Which.Should().Be("missingParameter");
            }

            [Fact]
            public void ShouldCheckQueryParametersAreOptional()
            {
                this.parser.ParseUrl(
                    "/literal{?query}",
                    new FakeParameter("query") { IsOptional = false });

                this.parser.QueryParameters.Should().BeEmpty();
                this.parser.ErrorParameters.Should().ContainSingle()
                    .Which.Should().Be("query");
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
            internal Dictionary<string, Type> QueryParameters { get; } = new Dictionary<string, Type>();

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

            protected override void OnCaptureParameter(Type parameterType, string name)
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

            protected override void OnQueryParameter(string name, Type parameterType)
            {
                this.QueryParameters[name] = parameterType;
            }
        }
    }
}
