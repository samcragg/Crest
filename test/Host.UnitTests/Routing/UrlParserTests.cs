namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UrlParserTests
    {
        private FakeUrlParser parser;

        [SetUp]
        public void SetUp()
        {
            this.parser = new FakeUrlParser();
        }

        [Test]
        public void GetSegmentsShouldReturnAllTheParts()
        {
            StringSegment[] segments = UrlParser.GetSegments("/one/two").ToArray();

            Assert.That(segments, Has.Length.EqualTo(2));
            Assert.That(segments[0].ToString(), Is.EqualTo("one"));
            Assert.That(segments[1].ToString(), Is.EqualTo("two"));
        }

        [Test]
        public void ShouldUnescapeBraces()
        {
            this.parser.ParseUrl("/{{escaped_braces}}/", new Dictionary<string, Type>());

            Assert.That(this.parser.Literals.Single(), Is.EqualTo("{escaped_braces}"));
        }

        [Test]
        public void ShouldCheckForMissingClosingBraces()
        {
            this.parser.ParseUrl("/{234/", new Dictionary<string, Type>());

            Assert.That(this.parser.ErrorParts.Single(), Is.EqualTo("4"));
        }

        [Test]
        public void ShouldCheckForUnescapedBraces()
        {
            this.parser.ParseUrl("/123}/", new Dictionary<string, Type>());
            this.parser.ParseUrl("/123{/", new Dictionary<string, Type>());

            Assert.That(this.parser.ErrorParts, Is.EqualTo(new[] { "}", "{" }));
        }

        [Test]
        public void ShouldCheckForValidParameters()
        {
            this.parser.ParseUrl("/{missingParameter}/", new Dictionary<string, Type>());

            Assert.That(this.parser.ErrorParts.Single(), Is.EqualTo("missingParameter"));
        }

        [Test]
        public void ShouldCheckForDuplicateParameters()
        {
            var parameters = new Dictionary<string, Type>
            {
                { "parameter", typeof(int) }
            };

            this.parser.ParseUrl("/{parameter}/{parameter}", parameters);

            Assert.That(this.parser.ErrorParameters.Single(), Is.EqualTo("parameter"));
        }

        [Test]
        public void ShouldCheckForUnmatchedParameters()
        {
            var parameters = new Dictionary<string, Type>
            {
                { "parameter", typeof(int) }
            };

            this.parser.ParseUrl("/literal", parameters);

            Assert.That(this.parser.ErrorParameters.Single(), Is.EqualTo("parameter"));
        }

        [Test]
        public void ShouldCaptureLiterals()
        {
            this.parser.ParseUrl("/literal/", new Dictionary<string, Type>());

            Assert.That(parser.Literals.Single(), Is.EqualTo("literal"));
        }

        [Test]
        public void ShouldCaptureParameters()
        {
            var parameters = new Dictionary<string, Type>
            {
                { "capture", typeof(int) }
            };

            this.parser.ParseUrl("/{capture}/", parameters);

            Assert.That(parser.Captures.Single().Item1, Is.EqualTo(typeof(int)));
            Assert.That(parser.Captures.Single().Item2, Is.EqualTo("capture"));
        }

        private class FakeUrlParser : UrlParser
        {
            private string routeUrl;

            internal List<Tuple<Type, string>> Captures { get; } = new List<Tuple<Type, string>>();

            internal List<string> ErrorParts { get; } = new List<string>();

            internal List<string> ErrorParameters { get; } = new List<string>();

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
