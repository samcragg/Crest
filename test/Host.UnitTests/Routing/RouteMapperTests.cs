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
        private static MethodInfo ExampleMethodInfo =
            typeof(RouteMapperTests).GetMethod(nameof(ExampleMethod), BindingFlags.Instance | BindingFlags.NonPublic);

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

            MethodInfo route = mapper.Match("GET", "/route", query, out parameters);
            MethodInfo unknown = mapper.Match("GET", "/unknown", query, out parameters);

            Assert.That(route, Is.Not.Null);
            Assert.That(unknown, Is.Null);
        }

        [Test]
        public void MatchShouldMatchTheVerb()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("PUT", "/route") });

            MethodInfo get = mapper.Match("GET", "/route", query, out parameters);
            MethodInfo put = mapper.Match("PUT", "/route", query, out parameters);

            Assert.That(get, Is.Null);
            Assert.That(put, Is.Not.Null);
        }

        [Test]
        public void MatchShouldSetTheParameters()
        {
            IReadOnlyDictionary<string, object> parameters = null;
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();
            var mapper = new RouteMapper(new[] { CreateRoute("GET", "/route") });

            mapper.Match("GET", "/route", query, out parameters);

            Assert.That(parameters, Is.Not.Null);
        }

        private static RouteMetadata CreateRoute(string verb, string route)
        {
            return new RouteMetadata
            {
                Factory = () => null,
                Method = ExampleMethodInfo,
                RouteUrl = route,
                Verb = verb
            };
        }

        // Cannot be static
        private Task ExampleMethod()
        {
            throw new NotImplementedException();
        }
    }
}
