namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class UrlParserTests
    {
        private FakeUrlParser parser;

        [SetUp]
        public void SetUp()
        {
            this.parser = new FakeUrlParser();
        }

        [TestFixture]
        public sealed class GetSegments : UrlParserTests
        {
            [Test]
            public void ShouldReturnAllTheParts()
            {
                StringSegment[] segments = UrlParser.GetSegments("/one/two").ToArray();

                segments.Should().HaveCount(2);
                segments[0].ToString().Should().Be("one");
                segments[1].ToString().Should().Be("two");
            }
        }

        [TestFixture]
        public sealed class ParseUrl : UrlParserTests
        {
            [Test]
            public void ShouldCaptureLiterals()
            {
                this.parser.ParseUrl("/literal/", new Dictionary<string, Type>());

                parser.Literals.Single().Should().Be("literal");
            }

            [Test]
            public void ShouldCaptureParameters()
            {
                var parameters = new Dictionary<string, Type>
            {
                { "capture", typeof(int) }
            };

                this.parser.ParseUrl("/{capture}/", parameters);

                parser.Captures.Single().Item1.Should().Be(typeof(int));
                parser.Captures.Single().Item2.Should().Be("capture");
            }

            [Test]
            public void ShouldCheckForDuplicateParameters()
            {
                var parameters = new Dictionary<string, Type>
            {
                { "parameter", typeof(int) }
            };

                this.parser.ParseUrl("/{parameter}/{parameter}", parameters);

                this.parser.ErrorParameters.Single().Should().Be("parameter");
            }

            [Test]
            public void ShouldCheckForMissingClosingBraces()
            {
                this.parser.ParseUrl("/{234/", new Dictionary<string, Type>());

                this.parser.ErrorParts.Single().Should().Be("4");
            }

            [Test]
            public void ShouldCheckForUnescapedBraces()
            {
                this.parser.ParseUrl("/123}/", new Dictionary<string, Type>());
                this.parser.ParseUrl("/123{/", new Dictionary<string, Type>());

                this.parser.ErrorParts.Should().BeEquivalentTo("}", "{");
            }

            [Test]
            public void ShouldCheckForUnmatchedParameters()
            {
                var parameters = new Dictionary<string, Type>
                {
                    { "parameter", typeof(int) }
                };

                this.parser.ParseUrl("/literal", parameters);

                this.parser.ErrorParameters.Single().Should().Be("parameter");
            }

            [Test]
            public void ShouldCheckForValidParameters()
            {
                this.parser.ParseUrl("/{missingParameter}/", new Dictionary<string, Type>());

                this.parser.ErrorParts.Single().Should().Be("missingParameter");
            }

            [Test]
            public void ShouldUnescapeBraces()
            {
                this.parser.ParseUrl("/{{escaped_braces}}/", new Dictionary<string, Type>());

                this.parser.Literals.Single().Should().Be("{escaped_braces}");
            }
        }

        private class FakeUrlParser : UrlParser
        {
            private string routeUrl;

            internal List<Tuple<Type, string>> Captures { get; } = new List<Tuple<Type, string>>();
            internal List<string> ErrorParameters { get; } = new List<string>();
            internal List<string> ErrorParts { get; } = new List<string>();
            internal List<string> Literals { get; } = new List<string>();

            internal override void ParseUrl(string routeUrl, IReadOnlyDictionary<string, Type> parameters)
            {
                this.routeUrl = routeUrl;
                base.ParseUrl(routeUrl, parameters);
            }

            protected override void OnCaptureSegment(Type parameterType, string name)
            {
                this.Captures.Add(Tuple.Create(parameterType, name));
            }

            protected override void OnError(string error, string parameter)
            {
                this.ErrorParameters.Add(parameter);
            }

            protected override void OnError(string error, int start, int length)
            {
                this.ErrorParts.Add(this.routeUrl.Substring(start, length));
            }

            protected override void OnLiteralSegment(string value)
            {
                this.Literals.Add(value);
            }
        }
    }
}
