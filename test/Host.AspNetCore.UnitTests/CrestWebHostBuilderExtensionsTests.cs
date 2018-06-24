namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using Xunit;

    public class CrestWebHostBuilderExtensionsTests
    {
        public sealed class UseCrest : CrestWebHostBuilderExtensionsTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => CrestWebHostBuilderExtensions.UseCrest(null);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldRegisterTheStartupClass()
            {
                IServiceCollection services = Substitute.For<IServiceCollection>();
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();
                builder.ConfigureServices(Arg.Do<Action<IServiceCollection>>(action => action(services)));

                CrestWebHostBuilderExtensions.UseCrest(builder);

                services.Received().Add(Arg.Is<ServiceDescriptor>(sd => sd.ServiceType == typeof(IStartup)));
            }

            [Fact]
            public void ShouldReturnThePassedInValue()
            {
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

                IWebHostBuilder result = CrestWebHostBuilderExtensions.UseCrest(builder);

                result.Should().BeSameAs(builder);
            }
        }
    }
}
