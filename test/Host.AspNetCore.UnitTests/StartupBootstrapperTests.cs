﻿namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Microsoft.AspNetCore.Builder;
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
                var builder = Substitute.For<IApplicationBuilder>();

                this.startup.Configure(builder);

                builder.ReceivedWithAnyArgs().Use(null);
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
