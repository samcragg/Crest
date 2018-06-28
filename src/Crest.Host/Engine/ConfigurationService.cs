// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;

    /// <summary>
    /// Provides instances of classes that have been marked as configuration.
    /// </summary>
    internal sealed class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationProvider[] providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="providers">The configuration providers to use.</param>
        public ConfigurationService(IEnumerable<IConfigurationProvider> providers)
        {
            this.providers = providers.OrderBy(p => p.Order).ToArray();
        }

        /// <inheritdoc />
        public bool CanConfigure(Type type)
        {
            return type.GetTypeInfo().IsDefined(typeof(ConfigurationAttribute), inherit: false);
        }

        /// <inheritdoc />
        public void InitializeInstance(object instance, IServiceProvider serviceProvider)
        {
            for (int i = 0; i < this.providers.Length; i++)
            {
                this.providers[i].Inject(instance);
            }
        }

        /// <inheritdoc />
        public Task InitializeProvidersAsync(IEnumerable<Type> discoveredTypes)
        {
            List<Type> knownTypes =
                discoveredTypes.Where(this.CanConfigure).ToList();

            var tasks = new Task[this.providers.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = this.providers[i].InitializeAsync(knownTypes);
            }

            return Task.WhenAll(tasks);
        }
    }
}
