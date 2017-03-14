namespace Host.UnitTests.Engine
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultConfigurationProviderTests
    {
        private DefaultConfigurationProvider provider;

        [SetUp]
        public void SetUp()
        {
            this.provider = new DefaultConfigurationProvider();
        }

        [TestFixture]
        public class Inject : DefaultConfigurationProviderTests
        {
            [Test]
            public async Task ShouldHandleTypesWithNoDefaultProperties()
            {
                await this.provider.Initialize(new[] { typeof(NoDefaultProperties) });
                var instance = new NoDefaultProperties();

                this.provider.Inject(instance);

                instance.String.Should().BeNull();
            }

            [Test]
            public async Task ShouldSetNestedProperties()
            {
                await this.provider.Initialize(new[] { typeof(HasNestedProperties) });
                var instance = new HasNestedProperties();

                this.provider.Inject(instance);

                instance.NotNullNested.Integer.Should().Be(HasDefaultProperties.IntegerDefault);
            }

            [Test]
            public async Task ShouldSetTheDefaultValuesOfProperties()
            {
                await this.provider.Initialize(new[] { typeof(HasDefaultProperties) });
                var instance = new HasDefaultProperties();

                this.provider.Inject(instance);

                instance.Integer.Should().Be(HasDefaultProperties.IntegerDefault);
            }

            private class HasDefaultProperties
            {
                internal const int IntegerDefault = 123;

                [DefaultValue(IntegerDefault)]
                public int Integer { get; set; }
            }

            private class HasNestedProperties
            {
                public HasDefaultProperties NotNullNested { get; } = new HasDefaultProperties();

                public HasDefaultProperties NullProperty { get; }
            }

            private class NoDefaultProperties
            {
                public string String { get; set; }
            }
        }

        [TestFixture]
        public class Order : DefaultConfigurationProviderTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                provider.Order.Should().BePositive();
            }
        }
    }
}
