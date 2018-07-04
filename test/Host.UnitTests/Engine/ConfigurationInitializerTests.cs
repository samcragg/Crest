namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Engine;
    using NSubstitute;
    using Xunit;

    public class ConfigurationInitializerTests
    {
        private readonly IConfigurationService configurationService;
        private readonly ConfigurationInitializer initializer;
        private readonly IServiceLocator serviceLocator;
        private readonly IServiceRegister serviceRegister;

        private ConfigurationInitializerTests()
        {
            this.configurationService = Substitute.For<IConfigurationService>();
            this.initializer = new ConfigurationInitializer(this.configurationService);
            this.serviceLocator = Substitute.For<IServiceLocator>();
            this.serviceRegister = Substitute.For<IServiceRegister>();
        }

        public sealed class InitializeAsync : ConfigurationInitializerTests
        {
            [Fact]
            public async Task ShouldInitializeTheConfigurationService()
            {
                this.serviceLocator.GetService(typeof(DiscoveredTypes))
                    .Returns(new DiscoveredTypes(new[] { typeof(string) }));

                await this.initializer.InitializeAsync(this.serviceRegister, this.serviceLocator);

                await this.configurationService.Received()
                    .InitializeProvidersAsync(Arg.Is<IEnumerable<Type>>(x => x.Single() == typeof(string)));
            }

            [Fact]
            public async Task ShouldRegisterTheInitializers()
            {
                this.serviceLocator.GetService(typeof(DiscoveredTypes))
                    .Returns(new DiscoveredTypes(new[] { typeof(object) }));

                // Force the passed in lambdas to be invoked
                object toInitialize = new object();
                this.serviceRegister.RegisterInitializer(
                    Arg.Do<Func<Type, bool>>(x => x(typeof(object))),
                    Arg.Do<Action<object>>(x => x(toInitialize)));

                await this.initializer.InitializeAsync(this.serviceRegister, this.serviceLocator);

                this.configurationService.Received().CanConfigure(typeof(object));
                this.configurationService.Received().InitializeInstance(toInitialize, this.serviceLocator);
            }
        }
    }
}
