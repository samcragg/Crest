// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using Crest.Abstractions;
    using Crest.Host;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyModel;

    /// <summary>
    /// Adapts the ASP .NET pipeline for the Crest framework.
    /// </summary>
    internal sealed class StartupBootstrapper : Bootstrapper, IStartup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupBootstrapper"/> class.
        /// </summary>
        /// <param name="context">The context for the instance.</param>
        public StartupBootstrapper(DependencyContext context)
        {
            this.SetDependencyContext(context);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupBootstrapper"/> class.
        /// </summary>
        /// <param name="locator">Used to locate the services.</param>
        /// <remarks>
        /// This constructor is required for unit testing only.
        /// </remarks>
        internal StartupBootstrapper(IServiceLocator locator)
            : base(locator)
        {
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
