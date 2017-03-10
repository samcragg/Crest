namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class StartupBootstrapperTests
    {
        private StartupBootstrapper startup;

        [SetUp]
        public void SetUp()
        {
            this.startup = new StartupBootstrapper(Substitute.For<IServiceRegister>());
        }

        [TestFixture]
        public sealed class Configure : StartupBootstrapperTests
        {
            [Test]
            public void ShouldRegisterARequestHandler()
            {
                var builder = Substitute.For<IApplicationBuilder>();

                this.startup.Configure(builder);

                builder.ReceivedWithAnyArgs().Use(null);
            }
        }

        [TestFixture]
        public sealed class ConfigureServices : StartupBootstrapperTests
        {
            [Test]
            public void ShouldReturnTheDefaultAspNetContainer()
            {
                IServiceProvider result = this.startup.ConfigureServices(Substitute.For<IServiceCollection>());

                result.Should().NotBeNull();
                result.GetType().FullName.Should().StartWith("Microsoft.");
            }
        }

        [TestFixture]
        public sealed class DefaultConstructor : StartupBootstrapperTests
        {
            [Test]
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
