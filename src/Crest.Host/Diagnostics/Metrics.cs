// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Crest.Abstractions;
    using Crest.Core.Logging;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;

    /// <summary>
    /// Allows the collection of metrics for the service.
    /// </summary>
    [SingleInstance]
    internal class Metrics
    {
        private static readonly ILog Logger = Log.For<Metrics>();

        private readonly AsyncLocal<RequestMetrics> currentMetrics =
            new AsyncLocal<RequestMetrics>();

        private readonly object instrumentLock = new object();
        private readonly Counter requestCount;
        private readonly Gauge requestSize;
        private readonly Gauge requestTime;
        private readonly Gauge responseSize;
        private readonly ITimeProvider time;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="time">Used to provide the current system time.</param>
        public Metrics(ITimeProvider time)
        {
            this.time = time;
            this.requestCount = new Counter();
            this.requestSize = new Gauge(time);
            this.requestTime = new Gauge(time);
            this.responseSize = new Gauge(time);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected Metrics()
        {
        }

        /// <summary>
        /// Called when a request is being matched.
        /// </summary>
        public virtual void BeginMatch()
        {
            this.currentMetrics.Value = new RequestMetrics
            {
                Start = this.time.GetCurrentMicroseconds(),
            };
        }

        /// <summary>
        /// Called when a request has been matched.
        /// </summary>
        /// <param name="request">The request data.</param>
        public virtual void BeginRequest(IRequestData request)
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.PreRequest = this.time.GetCurrentMicroseconds();
                metrics.RequestSize = GetContentLength(request.Headers);
            }
        }

        /// <summary>
        /// Called when the request is completed.
        /// </summary>
        /// <param name="written">The number of bytes written to the response.</param>
        public virtual void EndRequest(long written)
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.Complete = this.time.GetCurrentMicroseconds();
                metrics.ResponseSize = written;

                this.UpdateInstruments(metrics);

                Logger.Info(() => "Request sizes - " + metrics.GetSizes());
                Logger.Info(() => "Request timings - " + metrics.GetTimings());
            }
        }

        /// <summary>
        /// Records the time of the start of post-processing.
        /// </summary>
        public virtual void MarkStartPostProcessing()
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.PostRequest = this.time.GetCurrentMicroseconds();
            }
        }

        /// <summary>
        /// Records the time of the start of pre-processing.
        /// </summary>
        public virtual void MarkStartPreProcessing()
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.PreRequest = this.time.GetCurrentMicroseconds();
            }
        }

        /// <summary>
        /// Records the time of the start of processing.
        /// </summary>
        public virtual void MarkStartProcessing()
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.ProcessRequest = this.time.GetCurrentMicroseconds();
            }
        }

        /// <summary>
        /// Records the time of the start of writing the response.
        /// </summary>
        public virtual void MarkStartWriting()
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            if (metrics != null)
            {
                metrics.WriteResponse = this.time.GetCurrentMicroseconds();
            }
        }

        /// <summary>
        /// Writes the current metric information to the specified reporter.
        /// </summary>
        /// <param name="reporter">The instance to write to.</param>
        public virtual void WriteTo(IReporter reporter)
        {
            lock (this.instrumentLock)
            {
                reporter.Write("requestCount", this.requestCount, null);
                reporter.Write("requestTime", this.requestTime, TimeUnit.Instance);
                reporter.Write("requestSize", this.requestSize, BytesUnit.Instance);
                reporter.Write("responseSize", this.responseSize, BytesUnit.Instance);
            }
        }

        private static long GetContentLength(IReadOnlyDictionary<string, string> headers)
        {
            if (headers.TryGetValue("Content-Length", out string contentLength))
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    contentLength.AsSpan(),
                    long.MaxValue);

                if (result.IsSuccess)
                {
                    return (long)result.Value;
                }
            }

            return 0;
        }

        private void UpdateInstruments(RequestMetrics metrics)
        {
            lock (this.instrumentLock)
            {
                this.requestCount.Increment();
                this.requestSize.Add(metrics.RequestSize);
                this.requestTime.Add(metrics.Total);
                this.responseSize.Add(metrics.ResponseSize);
            }
        }
    }
}
