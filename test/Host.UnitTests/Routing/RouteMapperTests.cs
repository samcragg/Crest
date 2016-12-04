namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RouteMapperTests
    {
        private static readonly MethodInfo ExampleMethodInfo =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo ExampleMethod2Info =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod2), BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void GetAdapterShouldReturnAnAdapterForKnownMethods()
        {
            var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

            RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetAdapterShouldReturnNullForUnknownMethods()
        {
            var mapper = new RouteMapper(new RouteMetadata[0]);

            RouteMethod result = mapper.GetAdapter(ExampleMethodInfo);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void MatchShouldMatchTheRoute()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

            MethodInfo route = mapper.Match("GET", "/v1/route", query, out parameters);
            MethodInfo unknown = mapper.Match("GET", "/v1/unknown", query, out parameters);

            Assert.That(route, Is.Not.Null);
            Assert.That(unknown, Is.Null);
        }

        [Test]
        public void MatchShouldMatchTheVerb()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("PUT", "/route") });

            MethodInfo get = mapper.Match("GET", "/v1/route", query, out parameters);
            MethodInfo put = mapper.Match("PUT", "/v1/route", query, out parameters);

            Assert.That(get, Is.Null);
            Assert.That(put, Is.Not.Null);
        }

        [Test]
        public void MatchShouldMatchTheVersion()
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

            Assert.That(result, Is.SameAs(ExampleMethod2Info));
        }

        [Test]
        public void MatchShouldReturnNullWrongVersions()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route", 1, 1) });

            MethodInfo result = mapper.Match("GET", "/v2/route", query, out parameters);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void MatchShouldSetTheParameters()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

            mapper.Match("GET", "/v1/route", query, out parameters);

            Assert.That(parameters, Is.Not.Null);
        }

        [Test]
        public void ShouldCheckedForOverlappingMinimumVersions()
        {
            var routes = new[]
            {
                CreateRoute("GET", "/route", 1, 3),
                CreateRoute("GET", "/route", 3, 5),
            };

            Assert.That(
                () => new RouteMapper(routes),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void ShouldCheckedForOverlappingMaximumVersions()
        {
            var routes = new[]
            {
                CreateRoute("GET", "/route", 3, 5),
                CreateRoute("GET", "/route", 1, 3),
            };

            Assert.That(
                () => new RouteMapper(routes),
                Throws.InstanceOf<InvalidOperationException>());
        }

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
    }
}
