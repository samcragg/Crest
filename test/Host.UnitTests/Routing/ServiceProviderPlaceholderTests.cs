namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ServiceProviderPlaceholderTests
    {
        private readonly ServiceProviderPlaceholder placeholder = new ServiceProviderPlaceholder();

        public sealed class GetService : ServiceProviderPlaceholderTests
        {
            [Fact]
            public void ShouldReturnNullIfTheProviderIsNotSet()
            {
                this.placeholder.Provider = null;

                object result = this.placeholder.GetService(typeof(int));

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheObjectFromTheProviderProperty()
            {
                object instance = new object();
                this.placeholder.Provider = Substitute.For<IServiceProvider>();
                this.placeholder.Provider.GetService(typeof(int))
                    .Returns(instance);

                object result = this.placeholder.GetService(typeof(int));

                result.Should().BeSameAs(instance);
            }
        }
    }
}
