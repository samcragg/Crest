namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteMatcherTests
    {
        private static readonly MethodInfo ExampleMethod2Info =
            typeof(RouteMatcherTests).GetMethod(nameof(ExampleMethod2), BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo ExampleMethodInfo =
            typeof(RouteMatcherTests).GetMethod(nameof(ExampleMethod), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly ILookup<string, EndpointInfo<OverrideMethod>> noOverrides =
            Substitute.For<ILookup<string, EndpointInfo<OverrideMethod>>>();

        private static EndpointInfo<(string path, RouteMethodInfo method)> CreateEndpoint(
            string verb,
            string path,
            int from = 1,
            int to = 1,
            MethodInfo method = null,
            QueryCapture[] captures = null)
        {
            var routeMethod = new RouteMethodInfo(
                null,
                null,
                method ?? ExampleMethodInfo,
                captures ?? new QueryCapture[0]);
            return new EndpointInfo<(string, RouteMethodInfo)>(verb, (path, routeMethod), from, to);
        }

        private static Task<object> ExampleMethod(IReadOnlyDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        private static Task<object> ExampleMethod2(IReadOnlyDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        private RouteMatcher CreateMatcher(params string[] routes)
        {
            return this.CreateMatcher(routes.Select(x => CreateEndpoint("GET", x)).ToArray());
        }

        private RouteMatcher CreateMatcher(
            EndpointInfo<(string path, RouteMethodInfo method)>[] routes,
            int version = 1)
        {
            (MethodInfo, RouteMethod) CreateMethod(RouteMethodInfo info)
            {
                var adapter = (RouteMethod)info.Method.CreateDelegate(typeof(RouteMethod));
                return (info.Method, adapter);
            }

            return new RouteMatcher(
                routes.Select(x => CreateMethod(x.Value.method)).ToList(),
                new FakeTrie(routes.ToArray(), version),
                this.noOverrides);
        }

        public sealed class FindOverride : RouteMatcherTests
        {
            private readonly OverrideMethod overrideMethod = Substitute.For<OverrideMethod>();

            [Fact]
            public void ShouldMatchTheRoute()
            {
                RouteMatcher matcher = this.CreateMatcherFromOverrides(("GET", "/route"));

                OverrideMethod result = matcher.FindOverride("GET", "/route");

                result.Should().BeSameAs(this.overrideMethod);
            }

            [Fact]
            public void ShouldMatchTheVerb()
            {
                RouteMatcher matcher = this.CreateMatcherFromOverrides(("PUT", "/route"));

                matcher.FindOverride("GET", "/route").Should().BeNull();
                matcher.FindOverride("PUT", "/route").Should().BeSameAs(this.overrideMethod);
            }

            [Fact]
            public void ShouldReturnNullIfNoOverrideMatches()
            {
                RouteMatcher matcher = this.CreateMatcherFromOverrides(("GET", "/other"), ("PUT", "/route"));

                OverrideMethod result = matcher.FindOverride("GET", "/route");

                result.Should().BeNull();
            }

            private RouteMatcher CreateMatcherFromOverrides(params (string verb, string path)[] overrides)
            {
                return new RouteMatcher(
                    new (MethodInfo, RouteMethod)[0],
                    null,
                    overrides.ToLookup(
                        x => x.path,
                        x => new EndpointInfo<OverrideMethod>(x.verb, this.overrideMethod, 0, 0)));
            }
        }

        public sealed class GetAdapter : RouteMatcherTests
        {
            [Fact]
            public void ShouldReturnAnAdapterForKnownMethods()
            {
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("GET", "/route", method: ExampleMethodInfo),
                });

                RouteMethod result = matcher.GetAdapter(ExampleMethodInfo);

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldReturnNullForUnknownMethods()
            {
                RouteMatcher matcher = this.CreateMatcher();

                RouteMethod result = matcher.GetAdapter(ExampleMethodInfo);

                result.Should().BeNull();
            }
        }

        public sealed class GetKnownMethods : RouteMatcherTests
        {
            [Fact]
            public void ShouldReturnAllTheMethodsAdded()
            {
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("GET", "/route1", method: ExampleMethodInfo),
                    CreateEndpoint("GET", "/route2", method: ExampleMethod2Info),
                });

                IEnumerable<MethodInfo> result = matcher.GetKnownMethods();

                result.Should().Equal(ExampleMethodInfo, ExampleMethod2Info);
            }
        }

        public sealed class Match : RouteMatcherTests
        {
            private readonly ILookup<string, string> query = Substitute.For<ILookup<string, string>>();

            [Fact]
            public void ShouldIncludeQueryParameters()
            {
                QueryCapture capture = Substitute.For<QueryCapture>();
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("GET", "route", captures: new[] { capture })
                });

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                capture.Received().ParseParameters(this.query, (IDictionary<string, object>)result.Parameters);
            }

            [Fact]
            public void ShouldIncludeTheRequestBodyPlaceholder()
            {
                var routeMethod = new RouteMethodInfo("bodyParameter", typeof(int), ExampleMethodInfo, new QueryCapture[0]);
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    new EndpointInfo<(string, RouteMethodInfo)>("GET", ("route", routeMethod), 1, 1),
                });

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                result.Parameters.Keys.Should().Contain("bodyParameter");
            }

            [Fact]
            public void ShouldIncludeTheServiceProviderPlaceholder()
            {
                RouteMatcher matcher = this.CreateMatcher("route");

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                result.Parameters.Keys.Should().Contain(ServiceProviderPlaceholder.Key);
            }

            [Fact]
            public void ShouldMatchTheVerb()
            {
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("PUT", "route")
                });

                RouteMapperMatchResult get = matcher.Match("GET", "/route", this.query);
                RouteMapperMatchResult put = matcher.Match("PUT", "/route", this.query);

                get.Should().BeNull();
                put.Should().NotBeNull();
            }

            [Fact]
            public void ShouldMatchTheVersion()
            {
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("GET", "route", 1, 2, ExampleMethodInfo),
                    CreateEndpoint("GET", "route", 3, 4, ExampleMethod2Info),
                },
                version: 3);

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                result.Method.Should().BeSameAs(ExampleMethod2Info);
            }

            [Fact]
            public void ShouldReturnNullForWrongVersions()
            {
                RouteMatcher matcher = this.CreateMatcher(new[]
                {
                    CreateEndpoint("GET", "route", 1, 2),
                },
                version: 3);

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldSetTheParameters()
            {
                RouteMatcher matcher = this.CreateMatcher("route");

                RouteMapperMatchResult result = matcher.Match("GET", "/route", this.query);

                result.Parameters.Should().NotBeNull();
            }
        }

        private class FakeTrie : RouteTrie<EndpointInfo<RouteMethodInfo>>
        {
            private readonly EndpointInfo<RouteMethodInfo>[] endpoints;
            private readonly string[] paths;
            private readonly int version;

            internal FakeTrie(EndpointInfo<(string path, RouteMethodInfo method)>[] endpoints, int version)
            {
                this.endpoints = Array.ConvertAll(
                    endpoints,
                    e => new EndpointInfo<RouteMethodInfo>(e.Verb, e.Value.method, e.From, e.To));

                this.paths = Array.ConvertAll(endpoints, e => e.Value.path);
                this.version = version;
            }

            public override MatchResult Match(ReadOnlySpan<char> key)
            {
                var matched = new List<EndpointInfo<RouteMethodInfo>>();
                for (int i = 0; i < this.paths.Length; i++)
                {
                    if (this.paths[i] == key.ToString())
                    {
                        matched.Add(this.endpoints[i]);
                    }
                }

                if (matched.Count > 0)
                {
                    var captures = new Dictionary<string, object>
                    {
                        { VersionCaptureNode.KeyName, this.version },
                    };

                    return new MatchResult(captures, matched.ToArray());
                }
                else
                {
                    return default;
                }
            }
        }
    }
}
