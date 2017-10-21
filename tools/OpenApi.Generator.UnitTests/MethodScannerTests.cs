namespace OpenApi.Generator.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Core;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class MethodScannerTests
    {
        private MethodScannerTests()
        {
            Trace.SetUpTrace("quiet");
        }

        private Assembly CreateAssembly(params Type[] types)
        {
            Assembly assembly = Substitute.For<Assembly>();
            assembly.ExportedTypes.Returns(types);
            return assembly;
        }

        public sealed class MaximumVersion : MethodScannerTests
        {
            [Fact]
            public void ShouldReturnTheMaximumVersionSpecified()
            {
                Assembly assembly = this.CreateAssembly(typeof(VersionTests));
                var scanner = new MethodScanner(assembly);

                scanner.MaximumVersion.Should().Be(4);
            }
        }

        public sealed class MinimumVersion : MethodScannerTests
        {
            [Fact]
            public void ShouldReturnTheMinimumVersionOfAllTheRoutes()
            {
                Assembly assembly = this.CreateAssembly(typeof(VersionTests));
                var scanner = new MethodScanner(assembly);

                scanner.MinimumVersion.Should().Be(2);
            }
        }

        public sealed class Routes : MethodScannerTests
        {
            [Fact]
            public void ShouldIncludeAllTheRouteAttributesOnAMethod()
            {
                Assembly assembly = this.CreateAssembly(typeof(MultipleRoutes));
                var scanner = new MethodScanner(assembly);

                List<string> routes = scanner.Routes.Select(r => r.Route).ToList();

                routes.Should().HaveCount(2);
                routes.Should().BeEquivalentTo("route1", "route2");
            }

            [Fact]
            public void ShouldIncludeDeleteAttributes()
            {
                Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
                var scanner = new MethodScanner(assembly);

                RouteInformation route = scanner.Routes.Single(r => r.Verb == "delete");

                route.Method.Name.Should().Be(nameof(RouteTypesTests.Delete));
                route.Route.Should().Be("delete_route");
            }

            [Fact]
            public void ShouldIncludeGetAttributes()
            {
                Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
                var scanner = new MethodScanner(assembly);

                RouteInformation route = scanner.Routes.Single(r => r.Verb == "get");

                route.Method.Name.Should().Be(nameof(RouteTypesTests.Get));
                route.Route.Should().Be("get_route");
            }

            [Fact]
            public void ShouldIncludePostAttributes()
            {
                Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
                var scanner = new MethodScanner(assembly);

                RouteInformation route = scanner.Routes.Single(r => r.Verb == "post");

                route.Method.Name.Should().Be(nameof(RouteTypesTests.Post));
                route.Route.Should().Be("post_route");
            }

            [Fact]
            public void ShouldIncludePutAttributes()
            {
                Assembly assembly = this.CreateAssembly(typeof(RouteTypesTests));
                var scanner = new MethodScanner(assembly);

                RouteInformation route = scanner.Routes.Single(r => r.Verb == "put");

                route.Method.Name.Should().Be(nameof(RouteTypesTests.Put));
                route.Route.Should().Be("put_route");
            }

            [Fact]
            public void ShouldIncludeTheVerbIfTheVerbIsNotTheLastAttribute()
            {
                Assembly assembly = this.CreateAssembly(typeof(VersionTests));
                var scanner = new MethodScanner(assembly);

                RouteInformation route = scanner.Routes.First();

                route.Verb.Should().Be("get");
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
