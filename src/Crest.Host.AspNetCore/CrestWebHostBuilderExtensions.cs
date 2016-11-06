// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using Microsoft.AspNetCore.Hosting;

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
        /// <returns>
        /// The passed in instance to enable fluent configuration.
        /// </returns>
        [CLSCompliant(false)]
        public static IWebHostBuilder UseCrest(this IWebHostBuilder builder)
        {
            Check.IsNotNull(builder, nameof(builder));

            builder.UseStartup<StartupBootstrapper>();
            return builder;
        }
    }
}
