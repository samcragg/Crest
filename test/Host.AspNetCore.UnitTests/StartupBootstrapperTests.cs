namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StartupBootstrapperTests
    {
        private StartupBootstrapper startup;

        [SetUp]
        public void SetUp()
        {
            this.startup = new StartupBootstrapper();
        }
        [Test]
        public void ConfigureShouldRegisterARequestHandler()
        {
            var builder = Substitute.For<IApplicationBuilder>();

            this.startup.Configure(builder);

            builder.ReceivedWithAnyArgs().Use(null);
        }

        [Test]
        public void ConfigureServicesShouldReturnTheDefaultAspNetContainer()
        {
            IServiceProvider result = this.startup.ConfigureServices(Substitute.For<IServiceCollection>());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.GetType().FullName, Does.StartWith("Microsoft."));
        }
    }
}
