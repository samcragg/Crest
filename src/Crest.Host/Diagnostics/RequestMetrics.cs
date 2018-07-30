// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    /// <summary>
    /// Contains the timestamps of different stages of the request.
    /// </summary>
    internal sealed class RequestMetrics
    {
        /// <summary>
        /// Gets or sets the timestamp of when the request completed.
        /// </summary>
        public long Complete { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the post request handlers were
        /// invoked.
        /// </summary>
        public long PostRequest { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the pre-request handlers were
        /// invoked.
        /// </summary>
        public long PreRequest { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when processing was started.
        /// </summary>
        public long ProcessRequest { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes for the request.
        /// </summary>
        public long RequestSize { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes for the response.
        /// </summary>
        public long ResponseSize { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the request was started.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets the total amount of microseconds the request took.
        /// </summary>
        public long Total => this.Complete - this.Start;

        /// <summary>
        /// Gets or sets the timestamp of when writing was started.
        /// </summary>
        public long WriteResponse { get; set; }

        /// <summary>
        /// Gets the size information in a human readable format.
        /// </summary>
        /// <returns>A string containing the sizes.</returns>
        public string GetSizes()
        {
            return "Request: " + BytesUnit.Instance.Format(this.RequestSize) +
                ", Response: " + BytesUnit.Instance.Format(this.ResponseSize);
        }

        /// <summary>
        /// Gets the timings as a human displayable string.
        /// </summary>
        /// <returns>A string containing the timings.</returns>
        public string GetTimings()
        {
            using (var buffer = new StringBuffer())
            {
                AppendTime(buffer, "Match", this.Start, this.PreRequest);
                AppendTime(buffer, "Before", this.PreRequest, this.ProcessRequest);
                AppendTime(buffer, "Process", this.ProcessRequest, this.PostRequest);
                AppendTime(buffer, "After", this.PostRequest, this.WriteResponse);
                AppendTime(buffer, "Write", this.WriteResponse, this.Complete);
                AppendTime(buffer, "Total", this.Start, this.Complete);
                return buffer.ToString();
            }
        }

        private static void AppendTime(StringBuffer buffer, string label, long start, long end)
        {
            if (buffer.Length > 0)
            {
                buffer.Append(", ");
            }

            buffer.Append(label);
            buffer.Append(": ");
            buffer.Append(TimeUnit.Instance.Format(end - start));
        }
    }
}
