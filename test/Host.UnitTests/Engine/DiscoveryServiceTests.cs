namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host;
    using Crest.Host.Engine;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DiscoveryServiceTests
    {
        private DiscoveryService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new DiscoveryService(typeof(DiscoveryServiceTests).GetTypeInfo().Assembly);
        }

        [Test]
        public void GetCustomFactoriesShouldReturnInstancesOfTheITypeFactory()
        {
            IEnumerable<ITypeFactory> factories = this.service.GetCustomFactories();

            Assert.That(factories, Has.Some.InstanceOf<FakeTypeFactory>());
        }

        [Test]
        public void GetDiscoveredTypesShouldIncludeInternalTypes()
        {
            Assert.That(this.service.GetDiscoveredTypes(), Has.Member(typeof(ExampleInternalClass)));
        }

        [Test]
        public void GetRoutesShouldReturnAllTheRoutesOnAMethod()
        {
            IEnumerable<string> routes =
                this.service.GetRoutes(typeof(IHasRoutes))
                    .Where(rm => rm.Method.Name == nameof(IHasRoutes.MultipleRoutes))
                    .Select(rm => rm.RouteUrl);

            Assert.That(routes, Is.EquivalentTo(new[] { "Route1", "Route2" }));
        }

        [Test]
        public void GetRoutesShouldReturnTheVerb()
        {
            RouteMetadata metadata =
                this.service.GetRoutes(typeof(IHasRoutes))
                    .Where(rm => rm.Method.Name == nameof(IHasRoutes.DeleteMethod))
                    .Single();

            Assert.That(metadata.Verb, Is.EqualTo("DELETE").IgnoreCase);
        }

        [Test]
        public void GetRoutesShouldReturnTheVersionInformation()
        {
            RouteMetadata metadata =
                this.service.GetRoutes(typeof(IHasRoutes))
                    .Where(rm => rm.Method.Name == nameof(IHasRoutes.VersionedRoute))
                    .Single();

            Assert.That(metadata.MinimumVersion, Is.EqualTo(2));
            Assert.That(metadata.MaximumVersion, Is.EqualTo(3));
        }

        [Test]
        public void GetRoutesShouldNotAllowAMethodWithAMinimumVersionAfterItsMaximumVersion()
        {
            // Use ToList to force evaluation
            Assert.That(
                () => this.service.GetRoutes(typeof(IInvalidVersionRange)).ToList(),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void GetRoutesShouldNotAllowAMethodWithDifferentVerbs()
        {
            // Use ToList to force evaluation
            Assert.That(
                () => this.service.GetRoutes(typeof(IInvalidMixedAttributes)).ToList(),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void GetRoutesShouldNotAllowAMethodWithoutAVersion()
        {
            // Use ToList to force evaluation
            Assert.That(
                () => this.service.GetRoutes(typeof(IInvalidMissingVersion)).ToList(),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void IsSingleInstanceShouldReturnFalse()
        {
            // Out of the box everything is created per request
            Assert.That(this.service.IsSingleInstance(typeof(DiscoveryServiceTests)), Is.False);
        }

        [Test]
        public void ShouldHandleExceptionsWhenLoadingAssemblies()
        {
            this.service.AssemblyLoad = _ => { throw new BadImageFormatException(); };

            // Use ToList to force evaluation (just in case it changes to lazy
            // evaluation later on)
            List<Type> result = this.service.GetDiscoveredTypes().ToList();

            Assert.That(result, Is.Empty);
        }

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

        internal class ExampleInternalClass
        {
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
    }
}
