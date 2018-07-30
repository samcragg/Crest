// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;

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
        public HealthPageProvider(HealthPage page)
        {
            this.page = page;
        }

        /// <inheritdoc />
        public IEnumerable<DirectRouteMetadata> GetDirectRoutes()
        {
            OverrideMethod health = (request, _) =>
            {
                return Task.FromResult<IResponseData>(new ResponseData("text/html", 200, this.page.WriteToAsync));
            };

            yield return new DirectRouteMetadata { Method = health, RouteUrl = "/health", Verb = "GET" };
        }
    }
}
