// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using Crest.Host;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Adapts the ASP .NET pipeline for the Crest framework.
    /// </summary>
    internal sealed class StartupBootstrapper : Bootstrapper, IStartup
    {
        /// <summary>
        /// Gets or sets the optional service provider to use instead of the
        /// inherited one.
        /// </summary>
        /// <remarks>This is used for unit testing only.</remarks>
        internal IServiceProvider ServiceProviderOverride
        {
            get;
            set;
        }

        /// <inheritdoc />
        protected override IServiceProvider ServiceProvider
        {
            get { return this.ServiceProviderOverride ?? base.ServiceProvider; }
        }

        /// <inheritdoc />
        public void Configure(IApplicationBuilder app)
        {
            this.Initialize();
            var processor = new HttpContextProcessor(this);
            app.Use(_ => processor.HandleRequest);
        }

        /// <inheritdoc />
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }
}
