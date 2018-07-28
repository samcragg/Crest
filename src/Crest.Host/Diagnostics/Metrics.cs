// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Threading;
    using Crest.Abstractions;
    using Crest.Host.Engine;
    using Crest.Host.Logging;

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
        /// Called when a request is being matched.
        /// </summary>
        public void BeginMatch()
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
        public void BeginRequest(IRequestData request)
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            metrics.PreRequest = this.time.GetCurrentMicroseconds();
            metrics.RequestSize = request.Body.Length;
        }

        /// <summary>
        /// Called when the request is completed.
        /// </summary>
        /// <param name="written">The number of bytes written to the response.</param>
        public void EndRequest(long written)
        {
            RequestMetrics metrics = this.currentMetrics.Value;
            metrics.Complete = this.time.GetCurrentMicroseconds();
            metrics.ResponseSize = written;

            this.UpdateInstruments(metrics);

            Logger.Info(() => "Request sizes - " + metrics.GetSizes());
            Logger.Info(() => "Request timings - " + metrics.GetTimings());
        }

        /// <summary>
        /// Records the time of the start of post-processing.
        /// </summary>
        public void MarkStartPostProcessing()
        {
            this.currentMetrics.Value.PostRequest = this.time.GetCurrentMicroseconds();
        }

        /// <summary>
        /// Records the time of the start of pre-processing.
        /// </summary>
        public void MarkStartPreProcessing()
        {
            this.currentMetrics.Value.PreRequest = this.time.GetCurrentMicroseconds();
        }

        /// <summary>
        /// Records the time of the start of processing.
        /// </summary>
        public void MarkStartProcessing()
        {
            this.currentMetrics.Value.ProcessRequest = this.time.GetCurrentMicroseconds();
        }

        /// <summary>
        /// Records the time of the start of writing the response.
        /// </summary>
        public void MarkStartWriting()
        {
            this.currentMetrics.Value.WriteResponse = this.time.GetCurrentMicroseconds();
        }

        /// <summary>
        /// Writes the current metric information to the specified reporter.
        /// </summary>
        /// <param name="reporter">The instance to write to.</param>
        public void WriteTo(IReporter reporter)
        {
            lock (this.instrumentLock)
            {
                reporter.Write("requestCount", this.requestCount, null);
                reporter.Write("requestTime", this.requestTime, TimeUnit.Instance);
                reporter.Write("requestSize", this.requestSize, BytesUnit.Instance);
                reporter.Write("responseSize", this.responseSize, BytesUnit.Instance);
            }
        }

        private void UpdateInstruments(RequestMetrics metrics)
        {
            lock (this.instrumentLock)
            {
                this.requestCount.Increment();
                this.requestSize.Add(metrics.RequestSize);
                this.requestTime.Add(metrics.TotalMs);
                this.responseSize.Add(metrics.ResponseSize);
            }
        }
    }
}
