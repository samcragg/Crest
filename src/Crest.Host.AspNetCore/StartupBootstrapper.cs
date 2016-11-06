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
        /// <inheritdoc />
        public void Configure(IApplicationBuilder app)
        {
            var processor = new HttpContextProcessor();
            app.Use(_ => processor.HandleRequest);
        }

        /// <inheritdoc />
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }
}
