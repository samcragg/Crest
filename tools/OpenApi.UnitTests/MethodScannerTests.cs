namespace OpenApi.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Core;
    using Crest.OpenApi;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MethodScannerTests
    {
        [Test]
        public void MaximumVersionShouldReturnTheMaximumVersionSpecified()
        {
            Assembly assembly = this.CreateAssembly(typeof(VersionTests));
            var scanner = new MethodScanner(assembly);

            Assert.That(scanner.MaximumVersion, Is.EqualTo(4));
        }

        [Test]
        public void MinimumVersionShouldReturnTheMinimumVersionOfAllTheRoutes()
        {
            Assembly assembly = this.CreateAssembly(typeof(VersionTests));
            var scanner = new MethodScanner(assembly);

            Assert.That(scanner.MinimumVersion, Is.EqualTo(2));
        }

        [Test]
        public void RoutesShouldIncludeDeleteAttributes()
        {
            Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
            var scanner = new MethodScanner(assembly);

            RouteInformation route = scanner.Routes.Single(r => r.Verb == "delete");

            Assert.That(route.Method.Name, Is.EqualTo(nameof(RouteTypesTests.Delete)));
            Assert.That(route.Route, Is.EqualTo("delete_route"));
        }

        [Test]
        public void RoutesShouldIncludeGetAttributes()
        {
            Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
            var scanner = new MethodScanner(assembly);

            RouteInformation route = scanner.Routes.Single(r => r.Verb == "get");

            Assert.That(route.Method.Name, Is.EqualTo(nameof(RouteTypesTests.Get)));
            Assert.That(route.Route, Is.EqualTo("get_route"));
        }

        [Test]
        public void RoutesShouldIncludePostAttributes()
        {
            Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
            var scanner = new MethodScanner(assembly);

            RouteInformation route = scanner.Routes.Single(r => r.Verb == "post");

            Assert.That(route.Method.Name, Is.EqualTo(nameof(RouteTypesTests.Post)));
            Assert.That(route.Route, Is.EqualTo("post_route"));
        }

        [Test]
        public void RoutesShouldIncludePutAttributes()
        {
            Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
            var scanner = new MethodScanner(assembly);

            RouteInformation route = scanner.Routes.Single(r => r.Verb == "put");

            Assert.That(route.Method.Name, Is.EqualTo(nameof(RouteTypesTests.Put)));
            Assert.That(route.Route, Is.EqualTo("put_route"));
        }

        [Test]
        public void RoutesShouldIncludeAllTheRouteAttributesOnAMethod()
        {
            Assembly assembly = this.CreateAssembly(typeof(MultipleRoutes));
            var scanner = new MethodScanner(assembly);

            List<string> routes = scanner.Routes.Select(r => r.Route).ToList();

            Assert.That(routes, Has.Count.EqualTo(2));
            Assert.That(routes, Is.EquivalentTo(new[] { "route1", "route2" }));
        }

        [Test]
        public void RouteShouldIncludeTheVerbIfTheVerbIsNotTheLastAttribute()
        {
            Assembly assembly = this.CreateAssembly(typeof(VersionTests));
            var scanner = new MethodScanner(assembly);

            RouteInformation route = scanner.Routes.First();

            Assert.That(route.Verb, Is.EqualTo("get"));
        }

        private Assembly CreateAssembly(params Type[] types)
        {
            var assembly = Substitute.For<Assembly>();
            assembly.ExportedTypes.Returns(types);
            return assembly;
        }

        private class MultipleRoutes
        {
            [Get("route1")]
            [Get("route2")]
            public void MultipleGets() { }
        }

        private class RouteTypesTests
        {
            [Delete("delete_route")]
            public void Delete() { }

            [Get("get_route")]
            public void Get() { }

            [Post("post_route")]
            public void Post() { }

            [Put("put_route")]
            public void Put() { }
        }

        private class VersionTests
        {
            [Get("route")]
            [Version(3)]
            public void One() { }

            [Get("route")]
            [Version(2, 4)]
            public void Two() { }
        }
    }
}
