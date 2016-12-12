namespace Host.UnitTests.Engine
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DefaultConfigurationProviderTests
    {
        private DefaultConfigurationProvider provider;
        [SetUp]
        public void SetUp()
        {
            this.provider = new DefaultConfigurationProvider();
        }

        [Test]
        public void OrderShouldReturnAPositiveValue()
        {
            Assert.That(provider.Order, Is.Positive);
        }

        [Test]
        public async Task InjectShouldSetTheDefaultValuesOfProperties()
        {
            await this.provider.Initialize(new[] { typeof(HasDefaultProperties) });
            var instance = new HasDefaultProperties();

            this.provider.Inject(instance);

            Assert.That(instance.Integer, Is.EqualTo(HasDefaultProperties.IntegerDefault));
        }

        [Test]
        public async Task InjectShouldHandleTypesWithNoDefaultProperties()
        {
            await this.provider.Initialize(new[] { typeof(NoDefaultProperties) });
            var instance = new NoDefaultProperties();

            this.provider.Inject(instance);

            Assert.That(instance.String, Is.Null);
        }

        [Test]
        public async Task InjectShouldSetNestedProperties()
        {
            await this.provider.Initialize(new[] { typeof(HasNestedProperties) });
            var instance = new HasNestedProperties();

            this.provider.Inject(instance);

            Assert.That(instance.NotNullNested.Integer, Is.EqualTo(HasDefaultProperties.IntegerDefault));
        }

        private class HasDefaultProperties
        {
            internal const int IntegerDefault = 123;

            [DefaultValue(IntegerDefault)]
            public int Integer { get; set; }
        }

        private class NoDefaultProperties
        {
            public string String { get; set; }
        }

        private class HasNestedProperties
        {
            public HasDefaultProperties NotNullNested { get; } = new HasDefaultProperties();

            public HasDefaultProperties NullProperty { get; }
        }
    }
}
