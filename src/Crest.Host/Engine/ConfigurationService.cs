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
    using Crest.Core;

    /// <summary>
    /// Provides instances of classes that have been marked as configuration.
    /// </summary>
    public class ConfigurationService
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        protected ConfigurationService()
        {
            this.providers = new IConfigurationProvider[0];
        }

        /// <summary>
        /// Determines whether the specified type can be initialized with this
        /// service or not.
        /// </summary>
        /// <param name="type">The type information.</param>
        /// <returns>
        /// <c>true</c> if new instances of the specified type should be passed
        /// to <see cref="InitializeInstance(object, IServiceProvider)"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanConfigure(Type type)
        {
            return type.GetTypeInfo().IsDefined(typeof(ConfigurationAttribute), inherit: false);
        }

        /// <summary>
        /// Initializes the specified object by setting configuration options
        /// on its properties.
        /// </summary>
        /// <param name="instance">The object to initialize.</param>
        /// <param name="serviceProvider">Provides services at runtime.</param>
        public virtual void InitializeInstance(object instance, IServiceProvider serviceProvider)
        {
            for (int i = 0; i < this.providers.Length; i++)
            {
                this.providers[i].Inject(instance);
            }
        }

        /// <summary>
        /// Allows the providers to be initialized.
        /// </summary>
        /// <param name="discoveredTypes">
        /// All the types that have been discovered at runtime.
        /// </param>
        /// <returns>The result of the asynchronous operation.</returns>
        public virtual Task InitializeProviders(IEnumerable<Type> discoveredTypes)
        {
            List<Type> knownTypes =
                discoveredTypes.Where(this.CanConfigure).ToList();

            Task[] tasks = new Task[this.providers.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = this.providers[i].Initialize(knownTypes);
            }

            return Task.WhenAll(tasks);
        }
    }
}
