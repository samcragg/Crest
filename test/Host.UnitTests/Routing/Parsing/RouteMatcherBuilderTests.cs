namespace Host.UnitTests.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using Crest.Host.Routing.Parsing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteMatcherBuilderTests
    {
        private readonly FakeRouteMatcherBuilder builder = new FakeRouteMatcherBuilder();

        public sealed class AddMethod : RouteMatcherBuilderTests
        {
            [Fact]
            public void ShouldAddMethodsWithBodyParameters()
            {
                RouteMetadata route = CreateRoute<string>("/literal", 1, 1, "implicitBody");
                route.CanReadBody = true;

                this.builder.AddMethod(route);
                (_, EndpointInfo<RouteMethodInfo> endpoint) = this.GetRoutes().Single();

                endpoint.Value.BodyParameterName.Should().Be("implicitBody");
                endpoint.Value.BodyType.Should().Be(typeof(string));
            }

            [Fact]
            public void ShouldAddMethodsWithParameterCaptures()
            {
                this.builder.AddMethod(CreateRoute<bool>("/route/{test}", 1, 1, "test"));

                (IMatchNode[] nodes, _) = this.GetRoutes().Single();

                nodes.Should().HaveCount(2)
                    .And.Subject.Last().Should().BeOfType<BoolCaptureNode>();
            }

            [Fact]
            public void ShouldAddMethodsWithQueryCaptures()
            {
                this.builder.AddMethod(CreateRoute<string>("/route{?test}", 1, 1, "test"));

                (IMatchNode[] nodes, EndpointInfo<RouteMethodInfo> endpoint) = this.GetRoutes().Single();

                nodes.Should().ContainSingle();
                endpoint.Value.QueryCaptures.Should().ContainSingle()
                    .Which.ParameterName.Should().Be("test");
            }

            [Fact]
            public void ShouldAddTheMethodForTheRoute()
            {
                this.builder.AddMethod(CreateRoute("/route", 2, 3));

                (IMatchNode[] nodes, EndpointInfo<RouteMethodInfo> endpoint) = this.GetRoutes().Single();

                nodes.Should().ContainSingle()
                    .Which.Should().BeOfType<LiteralNode>()
                    .Which.Literal.Should().Be("/route");

                endpoint.From.Should().Be(2);
                endpoint.To.Should().Be(3);
            }



            [Fact]
            public void ShouldCaptureBooleans()
            {
                NodeMatchInfo match = this.GetMatchFor(typeof(bool), "true");

                match.Success.Should().BeTrue();
                match.Value.Should().Be(true);
            }

            [Fact]
            public void ShouldCaptureGuids()
            {
                const string GuidValue = "A000CEAB-610F-40E0-8A8D-9FDADB177809";
                NodeMatchInfo match = this.GetMatchFor(typeof(Guid), GuidValue);

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
                NodeMatchInfo match = this.GetMatchFor(type, value);

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType(type);
                match.Value.Should().Be(expected);
            }

            [Fact]
            public void ShouldCaptureTypesWithTypeConverters()
            {
                NodeMatchInfo match = this.GetMatchFor(typeof(CustomData), "custom_data");

                match.Success.Should().BeTrue();
                match.Value.Should().BeOfType<CustomData>()
                     .Which.Data.Should().Be("custom_data");
            }

            [Fact]
            public void ShouldConvertTheCapturedQueryValue()
            {
                RouteMetadata route = CreateRoute<bool?>("/route{?test}", 1, 1, "test");

                this.builder.AddMethod(route);
                (_, EndpointInfo<RouteMethodInfo> endpoint) = this.GetRoutes().Single();
                QueryCapture capture = endpoint.Value.QueryCaptures.Should().ContainSingle().Subject;

                ILookup<string, string> query = new[] { ("test", "true") }.ToLookup(x => x.Item1, x => x.Item2);
                var dictionary = new Dictionary<string, object>();
                capture.ParseParameters(query, dictionary);

                dictionary.Should().ContainKey("test")
                    .WhichValue.Should().Be(true);
            }

            [Fact]
            public void ShouldEnsureTheQueryCatchAllIsLast()
            {
                this.builder.AddMethod(CreateRoute<object>("/literal{?all*,capture}", 1, 1, "all", "capture"));

                (_, EndpointInfo<RouteMethodInfo> endpoint) = this.GetRoutes().Single();

                endpoint.Value.QueryCaptures.Should().HaveCount(2)
                    .And.Subject.Last().ParameterName.Should().Be("all");
            }

            [Fact]
            public void ShouldThrowFormatExceptionForCapturedFromBody()
            {
                RouteMetadata route = CreateRoute<object>("/CannotBeMarkedAsFromBody/{one}", 1, 1, "one");
                route.CanReadBody = true;
                MarkAsFromBody(route, 0);

                this.builder.Invoking(b => b.AddMethod(route))
                    .Should().Throw<FormatException>().WithMessage("*FromBody*captured*");
            }

            [Fact]
            public void ShouldThrowFormatExceptionForMultipleCatchAlls()
            {
                RouteMetadata route = CreateRoute<object>(
                    "/MultipleCatchAllParameters{?one*,two*}", 1, 1, "one", "two");

                this.builder.Invoking(b => b.AddMethod(route))
                    .Should().Throw<FormatException>().WithMessage("*multiple*catch-all*");
            }

            [Fact]
            public void ShouldThrowFormatExceptionForMultipleFromBody()
            {
                RouteMetadata route = CreateRoute<object>("/MultipleBodyParameters", 1, 1, "one", "two");
                route.CanReadBody = true;
                MarkAsFromBody(route, 0);
                MarkAsFromBody(route, 1);

                this.builder.Invoking(b => b.AddMethod(route))
                    .Should().Throw<FormatException>().WithMessage("*multiple*body*");
            }

            [Theory]
            [InlineData("/DuplicateParameter/{param}/{param}", "multiple")]
            [InlineData("/IncorrectCatchAllType{?param*}", "dynamic type")]
            [InlineData("/MissingClosingBrace/{param", "closing brace")]
            [InlineData("/MustBeOptional{?param}", "optional")]
            [InlineData("/ParameterNotFound", "missing")]
            [InlineData("/UnknownParameter/{x}", "parameter")]
            public void ShouldThrowFormatExceptionForParsingErrors(string url, string error)
            {
                RouteMetadata route = CreateRoute<int>(url, 1, 1, "param");

                // No need to test the parsing, as that's handled by UrlParser,
                // just test that we don't silently ignore parameter errors
                this.builder.Invoking(b => b.AddMethod(route))
                    .Should().Throw<FormatException>()
                    .WithMessage("*" + error + "*");
            }

            private static RouteMetadata CreateRoute<T>(string route, int min, int max, params string[] parameters)
            {
                return CreateRoute(route, min, max, typeof(T), parameters);
            }

            private static RouteMetadata CreateRoute(string route, int min, int max, Type type = null, params string[] parameters)
            {
                ParameterInfo CreateParameter(string name)
                {
                    ParameterInfo param = Substitute.For<ParameterInfo>();
                    param.Name.Returns(name);
                    param.ParameterType.Returns(type);
                    if (Nullable.GetUnderlyingType(type) != null)
                    {
                        param.Attributes.Returns(ParameterAttributes.Optional);
                    }

                    return param;
                }

                ParameterInfo[] fakeParameters = parameters.Select(CreateParameter).ToArray();
                MethodInfo method = Substitute.For<MethodInfo>();
                method.GetParameters().Returns(fakeParameters);

                return new RouteMetadata
                {
                    Path = route,
                    MaximumVersion = max,
                    MinimumVersion = min,
                    Method = method,
                };
            }

            private static void MarkAsFromBody(RouteMetadata route, int index)
            {
                route.Method.GetParameters()[index].GetCustomAttributes(typeof(FromBodyAttribute), Arg.Any<bool>())
                    .Returns(new[] { new FromBodyAttribute() });
            }

            private NodeMatchInfo GetMatchFor(Type type, string value)
            {
                this.builder.AddMethod(CreateRoute("{capture}", 1, 1, type, "capture"));
                (IMatchNode[] nodes, _) = this.GetRoutes().Should().ContainSingle().Subject;
                return nodes.Should().ContainSingle().Subject.Match(value.AsSpan());
            }

            private List<(IMatchNode[] nodes, EndpointInfo<RouteMethodInfo> endpoint)> GetRoutes()
            {
                this.builder.Build();

                var routes = new List<(IMatchNode[] nodes, EndpointInfo<RouteMethodInfo> endpoint)>();
                var stack = new Stack<IMatchNode>();
                this.builder.Routes.VisitNodes((d, n, e) =>
                {
                    // Ignore the first node, as it's just a container for the
                    // other nodes
                    if (d-- > 0)
                    {
                        while (d > stack.Count)
                        {
                            stack.Pop();
                        }

                        stack.Push(n);
                        foreach (EndpointInfo<RouteMethodInfo> endpoint in e)
                        {
                            routes.Add((stack.Reverse().ToArray(), endpoint));
                        }
                    }
                });

                return routes;
            }
        }

        public sealed class AddOverride : RouteMatcherBuilderTests
        {
            [Fact]
            public void ShouldAddTheOverrideToTheMatcher()
            {
                OverrideMethod method = Substitute.For<OverrideMethod>();

                this.builder.AddOverride("GET", "/route", method);

                ILookup<string, EndpointInfo<OverrideMethod>> overrides = this.GetOverrides();
                EndpointInfo<OverrideMethod> endpoint =
                    overrides["/route"].Should().ContainSingle().Subject;

                endpoint.Verb.Should().Be("GET");
                endpoint.Value.Should().BeSameAs(method);
            }

            [Fact]
            public void ShouldAllowMultipleOverridesThatDifferByVerb()
            {
                this.builder.AddOverride("GET", "/route", Substitute.For<OverrideMethod>());
                this.builder.AddOverride("PUT", "/route", Substitute.For<OverrideMethod>());

                ILookup<string, EndpointInfo<OverrideMethod>> overrides = this.GetOverrides();

                overrides.Should().ContainSingle()
                         .Which.Should().HaveCount(2);
            }

            [Fact]
            public void ShouldCheckOverridesAreUnique()
            {
                this.builder.AddOverride("GET", "/route", Substitute.For<OverrideMethod>());

                Action action = () => this.builder.AddOverride("GET", "/route", Substitute.For<OverrideMethod>());

                action.Should().Throw<InvalidOperationException>()
                      .WithMessage("*/route*");
            }

            private ILookup<string, EndpointInfo<OverrideMethod>> GetOverrides()
            {
                this.builder.Build();
                return this.builder.Overrides;
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

        private class FakeRouteClass
        {
            // Used to allow the route method to be created
        }

        private sealed class FakeRouteMatcherBuilder : RouteMatcherBuilder
        {
            public FakeRouteMatcherBuilder()
                : base(Substitute.For<RouteMethodAdapter>())
            {
            }

            internal IReadOnlyCollection<RouteMethod> Methods { get; private set; }

            internal ILookup<string, EndpointInfo<OverrideMethod>> Overrides { get; private set; }

            internal RouteTrie<EndpointInfo<RouteMethodInfo>> Routes { get; private set; }

            protected override RouteMatcher CreateMatcher(
                IReadOnlyCollection<RouteMethod> methods,
                RouteTrie<EndpointInfo<RouteMethodInfo>> routes,
                ILookup<string, EndpointInfo<OverrideMethod>> overrides)
            {
                this.Methods = methods;
                this.Overrides = overrides;
                this.Routes = routes;
                return base.CreateMatcher(methods, routes, overrides);
            }
        }
    }
}
