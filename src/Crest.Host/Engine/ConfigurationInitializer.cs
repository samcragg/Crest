// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Used to initialize the configuration service.
    /// </summary>
    internal sealed class ConfigurationInitializer : IStartupInitializer
    {
        private readonly IConfigurationService configurationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationInitializer"/> class.
        /// </summary>
        /// <param name="service">The service that will be initialized.</param>
        public ConfigurationInitializer(IConfigurationService service)
        {
            this.configurationService = service;
        }

        /// <inheritdoc />
        public async Task InitializeAsync(IServiceRegister serviceRegister, IServiceLocator serviceLocator)
        {
            var discovered = (DiscoveredTypes)serviceLocator.GetService(typeof(DiscoveredTypes));
            await this.configurationService.InitializeProvidersAsync(discovered.Types)
                      .ConfigureAwait(false);

            serviceRegister.RegisterInitializer(
                this.configurationService.CanConfigure,
                instance => this.configurationService.InitializeInstance(
                    instance,
                    serviceLocator));
        }
    }
}
