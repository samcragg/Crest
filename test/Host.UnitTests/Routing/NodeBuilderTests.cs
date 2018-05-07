﻿namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class NodeBuilderTests
    {
        private readonly NodeBuilder builder = new NodeBuilder();

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

        public sealed class Parse : NodeBuilderTests
        {
            [Fact]
            public void ShouldAllowDifferentVersionsOfTheSameRoute()
            {
                ParameterInfo param1 = CreateParameter<string>("param1");
                ParameterInfo param2 = CreateParameter<string>("param2");

                this.builder.Parse("1:1", "/{param1}/", new[] { param1 });

                // Not ambiguous as it's a different version
                Action action = () => this.builder.Parse("2:2", "/{param2}/", new[] { param2 });
                action.Should().NotThrow();
            }

            [Fact]
            public void ShouldAllowOverloadingByType()
            {
                ParameterInfo intParam = CreateParameter<int>("intParam");
                ParameterInfo stringParam = CreateParameter<string>("stringParam");

                this.builder.Parse("", "/{intParam}/", new[] { intParam });

                Action action = () => this.builder.Parse("", "/{stringParam}/", new[] { stringParam });
                action.Should().NotThrow();
            }

            [Fact]
            public void ShouldCaptureBooleans()
            {
                ParameterInfo capture = CreateParameter<bool>("capture");

                NodeMatchResult match = GetMatch(
                    this.builder.Parse("", "/{capture}/", new[] { capture }),
                    "true");

                match.Success.Should().BeTrue();
                match.Value.Should().Be(true);
            }

            [Fact]
            public void ShouldCaptureGuids()
            {
                var guid = Guid.NewGuid();
                ParameterInfo capture = CreateParameter<Guid>("capture");

                NodeMatchResult match = GetMatch(
                    this.builder.Parse("", "/{capture}/", new[] { capture }),
                    guid.ToString());

                match.Success.Should().BeTrue();
                match.Value.Should().Be(guid);
            }

            [Theory]
            [InlineData(typeof(byte), "1", (byte)1)]
            [InlineData(typeof(int), "1", (int)1)]
            [InlineData(typeof(long), "1", (long)1)]
            [InlineData(typeof(sbyte), "1", (sbyte)1)]
            [InlineData(typeof(short), "1", (short)1)]
            [InlineData(typeof(uint), "1", (uint)1)]
            [InlineData(typeof(ulong), "1", (ulong)1)]
            [InlineData(typeof(ushort), "1", (ushort)1)]
            public void ShouldCaptureIntegerTypes(Type type, string value, object expected)
            {
                ParameterInfo capture = CreateParameter(type, "capture");

                NodeMatchResult match = GetMatch(
                    this.builder.Parse("", "/{capture}/", new[] { capture }),
                    value);

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType(type);
                match.Value.Should().Be(expected);
            }

            [Fact]
            public void ShouldCaptureTypesWithTypeConverters()
            {
                ParameterInfo capture = CreateParameter<CustomData>("capture");

                NodeMatchResult match = GetMatch(
                    this.builder.Parse("", "/{capture}/", new[] { capture }),
                    "custom_data");

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType<CustomData>()
                     .Which.Data.Should().Be("custom_data");
            }

            [Fact]
            public void ShouldReturnNodesThatMatchTheRoute()
            {
                ParameterInfo captureParameter = CreateParameter<string>("capture");

                NodeBuilder.IParseResult result = this.builder.Parse("", "/literal/{capture}/", new[] { captureParameter });
                StringSegment[] segments = UrlParser.GetSegments("/literal/string_value").ToArray();

                NodeMatchResult literal = result.Nodes[0].Match(segments[0]);
                NodeMatchResult capture = result.Nodes[1].Match(segments[1]);

                result.Nodes.Should().HaveCount(2);
                literal.Success.Should().BeTrue();
                capture.Success.Should().BeTrue();
                capture.Value.Should().Be("string_value");
            }

            [Fact]
            public void ShouldReturnQueryCaptures()
            {
                ILookup<string, string> lookup = new[] { ("key", "value") }.ToLookup(x => x.Item1, x => x.Item2);
                var dictionary = new Dictionary<string, object>();
                ParameterInfo captureParameter = CreateParameter<string>("capture");
                captureParameter.Attributes.Returns(ParameterAttributes.Optional);

                NodeBuilder.IParseResult result = this.builder.Parse("", "/literal?key={capture}", new[] { captureParameter });
                QueryCapture query = result.QueryCaptures.Single();

                query.ParseParameters(lookup, dictionary);
                dictionary.Should().ContainKey("capture")
                          .WhichValue.Should().Be("value");
            }

            [Fact]
            public void ShouldThrowForAmbiguousRoutes()
            {
                ParameterInfo param1 = CreateParameter<string>("param1");
                ParameterInfo param2 = CreateParameter<string>("param2");

                this.builder.Parse("", "/{param1}/", new[] { param1 });

                // Although the parameter has a different name, it's the same type
                // so ambiguous
                Action action = () => this.builder.Parse("", "/{param2}/", new[] { param2 });

                action.Should().Throw<InvalidOperationException>();
            }

            [Theory]
            [InlineData("/{param}/{param}", "multiple")]
            [InlineData("/{param", "closing brace")]
            [InlineData("/route?queryKeyOnly", "query value")]
            [InlineData("/route?query={param}", "optional")]
            [InlineData("/route?query=literal", "capture")]
            [InlineData("/route", "missing")]
            [InlineData("/unescaped{brace", "brace")]
            [InlineData("/{unkownParameter}", "parameter")]
            public void ShouldThrowFormatExceptionForParsingErrors(string url, string error)
            {
                ParameterInfo param = CreateParameter<string>("param");

                // No need to test the parsing, as that's handled by UrlParse,
                // just test that we don't silently ignore parameter errors
                Action action = () => this.builder.Parse("", url, new[] { param });

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + error + "*");
            }

            private static NodeMatchResult GetMatch(NodeBuilder.IParseResult result, string value)
            {
                return result.Nodes.Single().Match(new StringSegment(value));
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
