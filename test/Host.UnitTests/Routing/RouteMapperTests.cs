namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteMapperTests
    {
        private static readonly MethodInfo BoolParameterMethodInfo =
            typeof(RouteMapperTests).GetMethod(nameof(BoolParameterMethod), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ExampleMethod2Info =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod2), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ExampleMethodInfo =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo MethodWithBodyParameterInfo =
            typeof(RouteMapperTests).GetMethod(nameof(MethodWithBodyParameter), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly IEnumerable<DirectRouteMetadata> noDirectRoutes = Enumerable.Empty<DirectRouteMetadata>();
        private readonly IEnumerable<RouteMetadata> noRoutes = Enumerable.Empty<RouteMetadata>();

        private static RouteMetadata CreateRoute(string verb, string route, int from = 1, int to = 1)
        {
            return new RouteMetadata
            {
                Factory = () => null,
                MaximumVersion = to,
                Method = ExampleMethodInfo,
                MinimumVersion = from,
                RouteUrl = route,
                Verb = verb
            };
        }

        // Cannot be static
        private Task BoolParameterMethod(bool parameter = false)
        {
            throw new NotImplementedException();
        }

        private Task ExampleMethod()
        {
            throw new NotImplementedException();
        }

        private Task ExampleMethod2()
        {
            throw new NotImplementedException();
        }

        private Task MethodWithBodyParameter(string body)
        {
            throw new NotImplementedException();
        }

        public sealed class Constructor : RouteMapperTests
        {
            [Fact]
            public void ShouldCheckedForOverlappingMaximumVersions()
            {
                RouteMetadata[] routes = new[]
                {
                    CreateRoute("GET", "/route", 3, 5),
                    CreateRoute("GET", "/route", 1, 3),
                };

                Action action = () => new RouteMapper(routes, this.noDirectRoutes);

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldCheckedForOverlappingMinimumVersions()
            {
                RouteMetadata[] routes = new[]
                {
                    CreateRoute("GET", "/route", 1, 3),
                    CreateRoute("GET", "/route", 3, 5),
                };

                Action action = () => new RouteMapper(routes, this.noDirectRoutes);

                action.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class FindOverride : RouteMapperTests
        {
            [Fact]
            public void ShouldMatchTheRoute()
            {
                OverrideMethod method = Substitute.For<OverrideMethod>();
                DirectRouteMetadata[] overrides = new[]
                {
                    new DirectRouteMetadata { Verb = "GET", RouteUrl = "/route", Method = method }
                };
                var mapper = new RouteMapper(this.noRoutes, overrides);

                OverrideMethod result = mapper.FindOverride("GET", "/route");

                result.Should().BeSameAs(method);
            }

            [Fact]
            public void ShouldMatchTheVerb()
            {
                OverrideMethod method = Substitute.For<OverrideMethod>();
                DirectRouteMetadata[] overrides = new[]
                {
                    new DirectRouteMetadata { Verb = "PUT", RouteUrl = "/route", Method = method }
                };
                var mapper = new RouteMapper(this.noRoutes, overrides);

                OverrideMethod result = mapper.FindOverride("GET", "/route");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfNoOverrideExists()
            {
                RouteMetadata[] routes = new[] { CreateRoute("GET", "/normal_route") };
                var mapper = new RouteMapper(routes, this.noDirectRoutes);

                OverrideMethod result = mapper.FindOverride("GET", "/normal_route");

                result.Should().BeNull();
            }
        }

        public sealed class GetAdapter : RouteMapperTests
        {
            [Fact]
            public void ShouldReturnAnAdapterForKnownMethods()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") }, this.noDirectRoutes);

                RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldReturnNullForUnknownMethods()
            {
                var mapper = new RouteMapper(this.noRoutes, this.noDirectRoutes);

                RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

                result.Should().BeNull();
            }
        }

        public sealed class GetKnownMethods : RouteMapperTests
        {
            [Fact]
            public void ShouldReturnAllTheMethodsAdded()
            {
                var mapper = new RouteMapper(
                    new[] { CreateRoute("GET", "/route1"), CreateRoute("PUT", "/route2") },
                    this.noDirectRoutes);

                IEnumerable<MethodInfo> result = mapper.GetKnownMethods();

                result.Should().Equal(ExampleMethodInfo, ExampleMethodInfo);
            }
        }

        public sealed class Match : RouteMapperTests
        {
            private readonly ILookup<string, string> query = Substitute.For<ILookup<string, string>>();

            [Fact]
            public void ShouldAllowKeyOnlyQueryParameterForBooleans()
            {
                this.query["boolean"].Returns(new[] { string.Empty });

                RouteMetadata[] routes = new[] { CreateRoute("GET", "/route?boolean={parameter}") };
                routes[0].Method = BoolParameterMethodInfo;

                var mapper = new RouteMapper(routes, this.noDirectRoutes);
                MethodInfo route = mapper.Match(
                    "GET",
                    "/v1/route",
                    this.query,
                    out IReadOnlyDictionary<string, object> parameters);

                parameters["parameter"].Should().Be(true);
            }

            [Fact]
            public void ShouldIncludeQueryParameters()
            {
                this.query["key"].Returns(new[] { "false" });

                RouteMetadata[] routes = new[] { CreateRoute("GET", "/route?key={parameter}") };
                routes[0].Method = BoolParameterMethodInfo;

                var mapper = new RouteMapper(routes, this.noDirectRoutes);
                MethodInfo route = mapper.Match(
                    "GET",
                    "/v1/route",
                    this.query,
                    out IReadOnlyDictionary<string, object> parameters);

                parameters["parameter"].Should().Be(false);
            }

            [Fact]
            public void ShouldIncludeTheRequestBodyPlaceholder()
            {
                RouteMetadata[] routes = new[] { CreateRoute("GET", "/route") };
                routes[0].CanReadBody = true;
                routes[0].Method = MethodWithBodyParameterInfo;

                var mapper = new RouteMapper(routes, this.noDirectRoutes);
                MethodInfo route = mapper.Match(
                    "GET",
                    "/v1/route",
                    this.query,
                    out IReadOnlyDictionary<string, object> parameters);

                parameters.Keys.Should().Contain("body");
            }

            [Fact]
            public void ShouldIncludeTheServiceProviderPlaceholder()
            {
                RouteMetadata[] routes = new[] { CreateRoute("GET", "/route") };

                var mapper = new RouteMapper(routes, this.noDirectRoutes);
                MethodInfo route = mapper.Match(
                    "GET",
                    "/v1/route",
                    this.query,
                    out IReadOnlyDictionary<string, object> parameters);

                parameters.Keys.Should().Contain(ServiceProviderPlaceholder.Key);
            }

            [Fact]
            public void ShouldMatchTheRoute()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") }, this.noDirectRoutes);

                MethodInfo route = mapper.Match("GET", "/v1/route", this.query, out _);
                MethodInfo unknown = mapper.Match("GET", "/v1/unknown", this.query, out _);

                route.Should().NotBeNull();
                unknown.Should().BeNull();
            }

            [Fact]
            public void ShouldMatchTheVerb()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("PUT", "/route") }, this.noDirectRoutes);

                MethodInfo get = mapper.Match("GET", "/v1/route", this.query, out _);
                MethodInfo put = mapper.Match("PUT", "/v1/route", this.query, out _);

                get.Should().BeNull();
                put.Should().NotBeNull();
            }

            [Fact]
            public void ShouldMatchTheVersion()
            {
                RouteMetadata[] routes = new[]
                {
                    CreateRoute("GET", "/route", 1, 2),
                    CreateRoute("GET", "/route", 3, 4)
                };
                routes[1].Method = ExampleMethod2Info;

                var mapper = new RouteMapper(routes, this.noDirectRoutes);

                MethodInfo result = mapper.Match("GET", "/v3/route", this.query, out _);

                result.Should().BeSameAs(ExampleMethod2Info);
            }

            [Fact]
            public void ShouldReturnNullForWrongVersions()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route", 1, 1) }, this.noDirectRoutes);

                MethodInfo result = mapper.Match("GET", "/v2/route", this.query, out _);

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldSetTheParameters()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") }, this.noDirectRoutes);
                mapper.Match(
                    "GET",
                    "/v1/route",
                    this.query,
                    out IReadOnlyDictionary<string, object> parameters);

                parameters.Should().NotBeNull();
            }
        }
    }
}
