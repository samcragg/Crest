namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class NodeBuilderTests
    {
        private readonly NodeBuilder builder = new NodeBuilder();

        private static RouteMetadata CreateRoute<T>(string route, int min, int max, params string[] parameters)
        {
            return CreateRoute(route, min, max, typeof(T), parameters);
        }

        private static RouteMetadata CreateRoute(string route, int min, int max, Type type, params string[] parameters)
        {
            ParameterInfo CreateParameter(string name)
            {
                ParameterInfo param = Substitute.For<ParameterInfo>();
                param.Name.Returns(name);
                param.ParameterType.Returns(type);
                return param;
            }

            ParameterInfo[] fakeParameters = parameters.Select(CreateParameter).ToArray();
            MethodInfo method = Substitute.For<MethodInfo>();
            method.GetParameters().Returns(fakeParameters);

            return new RouteMetadata
            {
                RouteUrl = route,
                MaximumVersion = max,
                MinimumVersion = min,
                Method = method
            };
        }

        public sealed class Parse : NodeBuilderTests
        {
            [Fact]
            public void ShouldAllowDifferentVersionsOfTheSameRoute()
            {
                RouteMetadata firstVersion = CreateRoute<string>("/{param1}/", 1, 1, "param1");
                RouteMetadata secondVersion = CreateRoute<string>("/{param2}/", 2, 2, "param2");

                this.builder.Parse(firstVersion);

                // Not ambiguous as it's a different version
                this.builder.Invoking(b => b.Parse(secondVersion))
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldAllowOverloadingByType()
            {
                RouteMetadata intRoute = CreateRoute<int>("/{intParam}/", 1, 1, "intParam");
                RouteMetadata stringRoute = CreateRoute<string>("/{stringParam}/", 1, 1, "stringParam");

                this.builder.Parse(intRoute);

                this.builder.Invoking(b => b.Parse(stringRoute))
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldCaptureBooleans()
            {
                NodeMatchResult match = this.GetMatchFor(typeof(bool), "true");

                match.Success.Should().BeTrue();
                match.Value.Should().Be(true);
            }

            [Fact]
            public void ShouldCaptureGuids()
            {
                const string GuidValue = "A000CEAB-610F-40E0-8A8D-9FDADB177809";
                NodeMatchResult match = this.GetMatchFor(typeof(Guid), GuidValue);

                match.Success.Should().BeTrue();
                match.Value.Should().Be(new Guid(GuidValue));
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
                NodeMatchResult match = this.GetMatchFor(type, value);

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType(type);
                match.Value.Should().Be(expected);
            }

            [Fact]
            public void ShouldCaptureTypesWithTypeConverters()
            {
                NodeMatchResult match = this.GetMatchFor(typeof(CustomData), "custom_data");

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType<CustomData>()
                     .Which.Data.Should().Be("custom_data");
            }

            [Fact]
            public void ShouldReturnNodesThatMatchTheRoute()
            {
                RouteMetadata route = CreateRoute<string>("/literal/{capture}/", 1, 1, "capture");

                NodeBuilder.IParseResult result = this.builder.Parse(route);
                StringSegment[] segments = UrlParser.GetSegments("/literal/string_value").ToArray();

                result.Nodes.Should().HaveCount(2);
                NodeMatchResult literal = result.Nodes[0].Match(segments[0]);
                NodeMatchResult capture = result.Nodes[1].Match(segments[1]);

                literal.Success.Should().BeTrue();
                capture.Success.Should().BeTrue();
                capture.Value.Should().Be("string_value");
            }

            [Fact]
            public void ShouldReturnQueryCaptures()
            {
                ILookup<string, string> lookup = new[] { ("key", "value") }.ToLookup(x => x.Item1, x => x.Item2);
                var dictionary = new Dictionary<string, object>();
                RouteMetadata route = CreateRoute<string>("/literal?key={capture}", 1, 1, "capture");
                route.Method.GetParameters()[0].Attributes.Returns(ParameterAttributes.Optional);

                NodeBuilder.IParseResult result = this.builder.Parse(route);
                QueryCapture query = result.QueryCaptures.Single();

                query.ParseParameters(lookup, dictionary);
                dictionary.Should().ContainKey("capture")
                          .WhichValue.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnTheBodyParameter()
            {
                RouteMetadata route = CreateRoute<string>("/", 1, 1, "bodyParameter");
                route.CanReadBody = true;

                NodeBuilder.IParseResult result = this.builder.Parse(route);

                result.BodyParameter.Value.Key.Should().Be("bodyParameter");
                result.BodyParameter.Value.Value.Should().Be(typeof(string));
            }

            [Fact]
            public void ShouldThrowForAmbiguousRoutes()
            {
                RouteMetadata firstVersion = CreateRoute<string>("/{param1}/", 1, 1, "param1");
                RouteMetadata secondVersion = CreateRoute<string>("/{param2}/", 1, 1, "param2");

                this.builder.Parse(firstVersion);

                // Although the parameter has a different name, it's the same type
                // so ambiguous
                this.builder.Invoking(b => b.Parse(secondVersion))
                    .Should().Throw<InvalidOperationException>();
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
                RouteMetadata route = CreateRoute<string>(url, 1, 1, "param");

                // No need to test the parsing, as that's handled by UrlParse,
                // just test that we don't silently ignore parameter errors
                this.builder.Invoking(b => b.Parse(route))
                    .Should().Throw<FormatException>()
                    .WithMessage("*" + error + "*");
            }

            private NodeMatchResult GetMatchFor(Type type, string value)
            {
                RouteMetadata route = CreateRoute("/{capture}/", 1, 1, type, "capture");
                NodeBuilder.IParseResult result = this.builder.Parse(route);
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
