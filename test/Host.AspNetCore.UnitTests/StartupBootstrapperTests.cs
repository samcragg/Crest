namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Abstractions;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using Xunit;

    public class StartupBootstrapperTests
    {
        private readonly StartupBootstrapper startup =
            new StartupBootstrapper(Substitute.For<IServiceRegister>());

        public sealed class Configure : StartupBootstrapperTests
        {
            [Fact]
            public void ShouldRegisterARequestHandler()
            {
                Func<RequestDelegate, RequestDelegate> useParameter = null;
                IApplicationBuilder builder = Substitute.For<IApplicationBuilder>();
                builder.Use(Arg.Do<Func<RequestDelegate, RequestDelegate>>(p => useParameter = p));

                this.startup.Configure(builder);
                RequestDelegate result = useParameter(Substitute.For<RequestDelegate>());

                result.Should().NotBeNull();
            }
        }

        public sealed class ConfigureServices : StartupBootstrapperTests
        {
            [Fact]
            public void ShouldReturnTheDefaultAspNetContainer()
            {
                IServiceProvider result = this.startup.ConfigureServices(Substitute.For<IServiceCollection>());

                result.Should().NotBeNull();
                result.GetType().FullName.Should().StartWith("Microsoft.");
            }
        }

        public sealed class DefaultConstructor : StartupBootstrapperTests
        {
            [Fact]
            public void ShouldSetTheServiceLocator()
            {
                using (var bootstrapper = new StartupBootstrapper())
                {
                    bootstrapper.ServiceLocator.Should().NotBeNull();
                }
            }
        }
    }
}
