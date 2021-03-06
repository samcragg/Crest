﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Engine;

    /// <summary>
    /// Allows the routing to the metrics data.
    /// </summary>
    internal sealed class MetricsProvider : IDirectRouteProvider
    {
        private readonly Metrics metrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsProvider"/> class.
        /// </summary>
        /// <param name="metrics">Contains the application metrics.</param>
        /// <param name="environment">The runtime environment.</param>
        /// <param name="options">The runtime options.</param>
        public MetricsProvider(
            Metrics metrics,
            HostingEnvironment environment,
            HostingOptions options)
        {
            // By default we should be enabled in development environments only
            // unless this has been configured
            if (options.DisplayMetrics.GetValueOrDefault(environment.IsDevelopment))
            {
                this.metrics = metrics;
            }
        }

        /// <inheritdoc />
        public IEnumerable<DirectRouteMetadata> GetDirectRoutes()
        {
            if (this.metrics != null)
            {
                yield return new DirectRouteMetadata
                {
                    Method = this.GetJsonAsync,
                    Path = "/metrics.json",
                    Verb = "GET",
                };
            }
        }

        private Task<IResponseData> GetJsonAsync(
            IRequestData request,
            IContentConverter converter)
        {
            byte[] jsonBytes = this.GetJsonData();
            return Task.FromResult<IResponseData>(new ResponseData(
                "application/json",
                (int)HttpStatusCode.OK,
                WriteResponseAsync));

            Task<long> WriteResponseAsync(Stream stream)
            {
                return stream.WriteAsync(jsonBytes, 0, jsonBytes.Length)
                    .ContinueWith(
                        _ => (long)jsonBytes.Length,
                        default,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Current);
            }
        }

        private byte[] GetJsonData()
        {
            using (var reporter = new JsonReporter())
            {
                this.metrics.WriteTo(reporter);
                string json = reporter.GenerateReport();
                return Encoding.UTF8.GetBytes(json);
            }
        }
    }
}
