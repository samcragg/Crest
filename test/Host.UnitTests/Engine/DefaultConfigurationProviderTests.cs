namespace Host.UnitTests.Engine
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Xunit;

    public class DefaultConfigurationProviderTests
    {
        private readonly DefaultConfigurationProvider provider = new DefaultConfigurationProvider();

        public class Inject : DefaultConfigurationProviderTests
        {
            [Fact]
            public async Task ShouldHandleTypesWithNoDefaultProperties()
            {
                await this.provider.InitializeAsync(new[] { typeof(NoDefaultProperties) });
                var instance = new NoDefaultProperties();

                this.provider.Inject(instance);

                instance.String.Should().BeNull();
            }

            [Fact]
            public async Task ShouldSetNestedProperties()
            {
                await this.provider.InitializeAsync(new[] { typeof(HasNestedProperties) });
                var instance = new HasNestedProperties();

                this.provider.Inject(instance);

                instance.NotNullNested.Integer.Should().Be(HasDefaultProperties.IntegerDefault);
            }

            [Fact]
            public async Task ShouldSetTheDefaultValuesOfProperties()
            {
                await this.provider.InitializeAsync(new[] { typeof(HasDefaultProperties) });
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

        public class Order : DefaultConfigurationProviderTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.provider.Order.Should().BePositive();
            }
        }
    }
}
