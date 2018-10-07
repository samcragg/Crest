// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Engine;

    /// <summary>
    /// Allows the routing to the health page.
    /// </summary>
    internal sealed class HealthPageProvider : IDirectRouteProvider
    {
        private readonly HealthPage page;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthPageProvider"/> class.
        /// </summary>
        /// <param name="page">The health page to return.</param>
        /// <param name="environment">The runtime environment.</param>
        /// <param name="options">The runtime options.</param>
        public HealthPageProvider(
            HealthPage page,
            HostingEnvironment environment,
            HostingOptions options)
        {
            // By default we should be enabled in development environments only
            // unless this has been configured
            if (options.DisplayHealth.GetValueOrDefault(environment.IsDevelopment))
            {
                this.page = page;
            }
        }

        /// <inheritdoc />
        public IEnumerable<DirectRouteMetadata> GetDirectRoutes()
        {
            OverrideMethod health = (request, _) =>
            {
                return Task.FromResult<IResponseData>(new ResponseData("text/html", 200, this.page.WriteToAsync));
            };

            if (this.page != null)
            {
                yield return new DirectRouteMetadata { Method = health, Path = "/health", Verb = "GET" };
            }
        }
    }
}
