namespace Host.UnitTests.Routing
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class NodeBuilderTests
    {
        private NodeBuilder builder;

        [SetUp]
        public void SetUp()
        {
            this.builder = new NodeBuilder();
        }

        private static ParameterInfo CreateParameter<T>(string name)
        {
            return CreateParameter(typeof(T), name);
        }

        private static ParameterInfo CreateParameter(Type type, string name)
        {
            ParameterInfo param = Substitute.For<ParameterInfo>();
            param.Name.Returns(name);
            param.ParameterType.Returns(type);
            return param;
        }

        [TestFixture]
        public sealed class Parse : NodeBuilderTests
        {
            [Test]
            public void ShouldAllowDifferentVersionsOfTheSameRoute()
            {
                ParameterInfo param1 = CreateParameter<string>("param1");
                ParameterInfo param2 = CreateParameter<string>("param2");

                this.builder.Parse("1:1", "/{param1}/", new[] { param1 });

                // Not ambiguous as it's a different version
                Action action = () => this.builder.Parse("2:2", "/{param2}/", new[] { param2 });
                action.ShouldNotThrow();
            }

            [Test]
            public void ShouldAllowOverloadingByType()
            {
                ParameterInfo intParam = CreateParameter<int>("intParam");
                ParameterInfo stringParam = CreateParameter<string>("stringParam");

                this.builder.Parse("", "/{intParam}/", new[] { intParam });

                Action action = () => this.builder.Parse("", "/{stringParam}/", new[] { stringParam });
                action.ShouldNotThrow();
            }

            [Test]
            public void ShouldCaptureBooleans()
            {
                ParameterInfo capture = CreateParameter<bool>("capture");

                IMatchNode[] nodes = this.builder.Parse("", "/{capture}/", new[] { capture });
                StringSegment segment = new StringSegment("true", 0, 4);
                NodeMatchResult match = nodes.Single().Match(segment);

                match.Success.Should().BeTrue();
                match.Value.Should().Be(true);
            }

            [Test]
            public void ShouldCaptureGuids()
            {
                Guid guid = Guid.NewGuid();
                ParameterInfo capture = CreateParameter<Guid>("capture");

                IMatchNode[] nodes = this.builder.Parse("", "/{capture}/", new[] { capture });
                StringSegment segment = new StringSegment(guid.ToString(), 0, 36);
                NodeMatchResult match = nodes.Single().Match(segment);

                match.Success.Should().BeTrue();
                match.Value.Should().Be(guid);
            }

            [TestCase(typeof(byte), "1", ExpectedResult = (byte)1)]
            [TestCase(typeof(int), "1", ExpectedResult = (int)1)]
            [TestCase(typeof(long), "1", ExpectedResult = (long)1)]
            [TestCase(typeof(sbyte), "1", ExpectedResult = (sbyte)1)]
            [TestCase(typeof(short), "1", ExpectedResult = (short)1)]
            [TestCase(typeof(uint), "1", ExpectedResult = (uint)1)]
            [TestCase(typeof(ulong), "1", ExpectedResult = (ulong)1)]
            [TestCase(typeof(ushort), "1", ExpectedResult = (ushort)1)]
            public object ShouldCaptureIntegerTypes(Type type, string value)
            {
                ParameterInfo capture = CreateParameter(type, "capture");

                IMatchNode[] nodes = this.builder.Parse("", "/{capture}/", new[] { capture });
                StringSegment segment = new StringSegment(value, 0, value.Length);
                NodeMatchResult match = nodes.Single().Match(segment);

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType(type);
                return match.Value;
            }

            [Test]
            public void ShouldCaptureTypesWithTypeConverters()
            {
                ParameterInfo capture = CreateParameter<CustomData>("capture");

                IMatchNode[] nodes = this.builder.Parse("", "/{capture}/", new[] { capture });
                StringSegment segment = new StringSegment("custom_data", 0, 11);
                NodeMatchResult match = nodes.Single().Match(segment);

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType<CustomData>()
                     .Which.Data.Should().Be("custom_data");
            }

            [Test]
            public void ShouldReturnNodesThatMatchTheRoute()
            {
                ParameterInfo captureParameter = CreateParameter<string>("capture");

                IMatchNode[] nodes = this.builder.Parse("", "/literal/{capture}/", new[] { captureParameter });
                StringSegment[] segments = UrlParser.GetSegments("/literal/string_value").ToArray();

                NodeMatchResult literal = nodes[0].Match(segments[0]);
                NodeMatchResult capture = nodes[1].Match(segments[1]);

                nodes.Should().HaveCount(2);
                literal.Success.Should().BeTrue();
                capture.Success.Should().BeTrue();
                capture.Value.Should().Be("string_value");
            }

            [Test]
            public void ShouldThrowForAmbiguousRoutes()
            {
                ParameterInfo param1 = CreateParameter<string>("param1");
                ParameterInfo param2 = CreateParameter<string>("param2");

                this.builder.Parse("", "/{param1}/", new[] { param1 });

                // Although the parameter has a different name, it's the same type
                // so ambiguous
                Action action = () => this.builder.Parse("", "/{param2}/", new[] { param2 });

                action.ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public void ShouldThrowFormatExceptionForParsingErrors()
            {
                // No need to test the parsing, as that's handled by UrlParse,
                // just test that we don't silently ignore errors
                Action action = () => this.builder.Parse("", "{missing brace", new ParameterInfo[0]);

                action.ShouldThrow<FormatException>();
            }

            [Test]
            public void ShouldThrowFormatExceptionForParsingParemeterErrors()
            {
                ParameterInfo param = CreateParameter<string>("param");

                // No need to test the parsing, as that's handled by UrlParse,
                // just test that we don't silently ignore parameter errors
                Action action = () => this.builder.Parse("", "/unused_parameter/", new[] { param });

                action.ShouldThrow<FormatException>();
            }
        }

        [TypeConverter(typeof(CustomDataConverter))]
        private class CustomData
        {
            public string Data { get; set; }
        }

        private class CustomDataConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return true;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return new CustomData { Data = value?.ToString() };
            }
        }
    }
}
