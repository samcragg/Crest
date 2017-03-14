namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class DiscoveryServiceTests
    {
        private DiscoveryService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new DiscoveryService(typeof(DiscoveryServiceTests).GetTypeInfo().Assembly);
        }

        public class FakeTypeFactory : ITypeFactory
        {
            public bool CanCreate(Type type)
            {
                return false;
            }

            public object Create(Type type, IServiceProvider serviceProvider)
            {
                return null;
            }
        }

        [TestFixture]
        public sealed class GetCustomFactories : DiscoveryServiceTests
        {
            [Test]
            public void ShouldReturnInstancesOfTheITypeFactory()
            {
                IEnumerable<ITypeFactory> factories = this.service.GetCustomFactories();

                factories.Should().Contain(f => f is FakeTypeFactory);
            }
        }

        [TestFixture]
        public sealed class GetDiscoveredTypes : DiscoveryServiceTests
        {
            [Test]
            public void ShouldHandleExceptionsWhenLoadingAssemblies()
            {
                this.service.AssemblyLoad = _ => { throw new BadImageFormatException(); };

                // Use ToList to force evaluation (just in case it changes to lazy
                // evaluation later on)
                List<Type> result = this.service.GetDiscoveredTypes().ToList();

                result.Should().BeEmpty();
            }

            [Test]
            public void ShouldIncludeInternalTypes()
            {
                IEnumerable<Type> types = this.service.GetDiscoveredTypes();

                types.Should().Contain(typeof(ExampleInternalClass));
            }

            internal class ExampleInternalClass
            {
            }
        }

        [TestFixture]
        public sealed class GetRoutes : DiscoveryServiceTests
        {
            internal interface IHasRoutes
            {
                [Delete("Route")]
                [Version(1)]
                Task DeleteMethod();

                [Get("Route1")]
                [Get("Route2")]
                [Version(1)]
                Task MultipleRoutes();

                [Get("Version")]
                [Version(2, 3)]
                Task VersionedRoute();
            }

            private interface IInvalidMissingVersion
            {
                [Get("Route")]
                Task NoVersion();
            }

            private interface IInvalidMixedAttributes
            {
                [Delete("Route1")]
                [Get("Route2")]
                [Version(1)]
                Task DifferentVerbs();
            }

            private interface IInvalidVersionRange
            {
                [Get("Route")]
                [Version(5, 3)]
                Task InvalidVersionRange();
            }

            [Test]
            public void ShouldNotAllowAMethodWithAMinimumVersionAfterItsMaximumVersion()
            {
                // Use ToList to force evaluation
                Action action = () => this.service.GetRoutes(typeof(IInvalidVersionRange)).ToList();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public void ShouldNotAllowAMethodWithDifferentVerbs()
            {
                // Use ToList to force evaluation
                Action action = () => this.service.GetRoutes(typeof(IInvalidMixedAttributes)).ToList();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public void ShouldNotAllowAMethodWithoutAVersion()
            {
                // Use ToList to force evaluation
                Action action = () => this.service.GetRoutes(typeof(IInvalidMissingVersion)).ToList();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public void ShouldReturnAllTheRoutesOnAMethod()
            {
                IEnumerable<string> routes =
                    this.service.GetRoutes(typeof(IHasRoutes))
                        .Where(rm => rm.Method.Name == nameof(IHasRoutes.MultipleRoutes))
                        .Select(rm => rm.RouteUrl);

                routes.Should().BeEquivalentTo("Route1", "Route2");
            }

            [Test]
            public void ShouldReturnTheVerb()
            {
                RouteMetadata metadata =
                    this.service.GetRoutes(typeof(IHasRoutes))
                        .Where(rm => rm.Method.Name == nameof(IHasRoutes.DeleteMethod))
                        .Single();

                metadata.Verb.Should().BeEquivalentTo("DELETE");
            }

            [Test]
            public void ShouldReturnTheVersionInformation()
            {
                RouteMetadata metadata =
                    this.service.GetRoutes(typeof(IHasRoutes))
                        .Where(rm => rm.Method.Name == nameof(IHasRoutes.VersionedRoute))
                        .Single();

                metadata.MinimumVersion.Should().Be(2);
                metadata.MaximumVersion.Should().Be(3);
            }
        }

        [TestFixture]
        public sealed class IsSingleInstance : DiscoveryServiceTests
        {
            [Test]
            public void ShouldReturnFalse()
            {
                // Out of the box everything is created per request
                bool result = this.service.IsSingleInstance(typeof(DiscoveryServiceTests));

                result.Should().BeFalse();
            }
        }
    }
}
