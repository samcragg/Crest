// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using Crest.Core.Util;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyModel;

    /// <summary>
    /// Allows the Crest hosting framework to be added to the application.
    /// </summary>
    public static class CrestWebHostBuilderExtensions
    {
        /// <summary>
        /// Adds the Crest hosting framework.
        /// </summary>
        /// <param name="builder">
        /// The application configuration request pipeline.
        /// </param>
        /// <param name="context">
        /// The optional dependency context to use when discovering types.
        /// </param>
        /// <returns>
        /// The passed in instance to enable fluent configuration.
        /// </returns>
        [CLSCompliant(false)]
        public static IWebHostBuilder UseCrest(this IWebHostBuilder builder, DependencyContext context = null)
        {
            Check.IsNotNull(builder, nameof(builder));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(typeof(IStartup), new StartupBootstrapper(context));
            });

            return builder;
        }
    }
}
