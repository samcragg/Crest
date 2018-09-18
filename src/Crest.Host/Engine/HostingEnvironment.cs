// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using Crest.Core.Logging;

    /// <summary>
    /// Contains information about the current environment the application is
    /// running under.
    /// </summary>
    [SingleInstance] // Set once at the start of the program
    internal class HostingEnvironment
    {
        private static readonly ILog Logger = Log.For<HostingEnvironment>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HostingEnvironment"/> class.
        /// </summary>
        public HostingEnvironment()
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environment))
            {
                // The environment defaults to production if it's not specified:
                // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments
                Logger.Warn("ASPNETCORE_ENVIRONMENT is not set, defaulting to Production");
                this.IsProduction = true;
                this.Name = "Production";
            }
            else
            {
                Logger.InfoFormat("Environment detected as '{environment}'", environment);
                this.IsDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
                this.IsProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
                this.IsStaging = string.Equals(environment, "Staging", StringComparison.OrdinalIgnoreCase);
                this.Name = environment;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current hosting environment
        /// name is Development.
        /// </summary>
        public virtual bool IsDevelopment { get; }

        /// <summary>
        /// Gets a value indicating whether the current hosting environment
        /// name is Production.
        /// </summary>
        public virtual bool IsProduction { get; }

        /// <summary>
        /// Gets a value indicating whether the current hosting environment
        /// name is Staging.
        /// </summary>
        public virtual bool IsStaging { get; }

        /// <summary>
        /// Gets the name of the current hosting environment.
        /// </summary>
        public virtual string Name { get; }
    }
}
