namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class RouteMapperTests
    {
        private static readonly MethodInfo ExampleMethod2Info =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod2), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ExampleMethodInfo =
                    typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod), BindingFlags.Instance | BindingFlags.NonPublic);

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
        private Task ExampleMethod()
        {
            throw new NotImplementedException();
        }

        private Task ExampleMethod2()
        {
            throw new NotImplementedException();
        }

        [TestFixture]
        public sealed class Constructor : RouteMapperTests
        {
            [Test]
            public void ShouldCheckedForOverlappingMaximumVersions()
            {
                var routes = new[]
                {
                    CreateRoute("GET", "/route", 3, 5),
                    CreateRoute("GET", "/route", 1, 3),
                };

                Action action = () => new RouteMapper(routes);

                action.ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public void ShouldCheckedForOverlappingMinimumVersions()
            {
                var routes = new[]
                {
                    CreateRoute("GET", "/route", 1, 3),
                    CreateRoute("GET", "/route", 3, 5),
                };

                Action action = () => new RouteMapper(routes);

                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [TestFixture]
        public sealed class GetAdapter : RouteMapperTests
        {
            [Test]
            public void ShouldReturnAnAdapterForKnownMethods()
            {
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

                RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

                result.Should().NotBeNull();
            }

            [Test]
            public void ShouldReturnNullForUnknownMethods()
            {
                var mapper = new RouteMapper(new RouteMetadata[0]);

                RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

                result.Should().BeNull();
            }
        }

        [TestFixture]
        public sealed class Match : RouteMapperTests
        {
            [Test]
            public void ShouldMatchTheRoute()
            {
                IReadOnlyDictionary<string, object> parameters = null;
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

                MethodInfo route = mapper.Match("GET", "/v1/route", query, out parameters);
                MethodInfo unknown = mapper.Match("GET", "/v1/unknown", query, out parameters);

                route.Should().NotBeNull();
                unknown.Should().BeNull();
            }

            [Test]
            public void ShouldMatchTheVerb()
            {
                IReadOnlyDictionary<string, object> parameters = null;
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
                var mapper = new RouteMapper(new[] { CreateRoute("PUT", "/route") });

                MethodInfo get = mapper.Match("GET", "/v1/route", query, out parameters);
                MethodInfo put = mapper.Match("PUT", "/v1/route", query, out parameters);

                get.Should().BeNull();
                put.Should().NotBeNull();
            }

            [Test]
            public void ShouldMatchTheVersion()
            {
                var routes = new[]
                {
                    CreateRoute("GET", "/route", 1, 2),
                    CreateRoute("GET", "/route", 3, 4)
                };
                routes[1].Method = ExampleMethod2Info;

                IReadOnlyDictionary<string, object> parameters = null;
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
                var mapper = new RouteMapper(routes);

                MethodInfo result = mapper.Match("GET", "/v3/route", query, out parameters);

                result.Should().BeSameAs(ExampleMethod2Info);
            }

            [Test]
            public void ShouldReturnNullWrongVersions()
            {
                IReadOnlyDictionary<string, object> parameters = null;
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route", 1, 1) });

                MethodInfo result = mapper.Match("GET", "/v2/route", query, out parameters);

                result.Should().BeNull();
            }

            [Test]
            public void ShouldSetTheParameters()
            {
                IReadOnlyDictionary<string, object> parameters = null;
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
                var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

                mapper.Match("GET", "/v1/route", query, out parameters);

                parameters.Should().NotBeNull();
            }
        }
    }
}
