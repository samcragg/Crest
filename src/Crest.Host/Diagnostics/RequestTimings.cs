// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Contains the timestamps of different stages of the request.
    /// </summary>
    internal sealed class RequestTimings
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
        /// Gets or sets the timestamp of when the request was started.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets the total amount of milliseconds the request took.
        /// </summary>
        public double TotalMs => (this.Complete - this.Start) / 1000.0;

        /// <summary>
        /// Gets or sets the timestamp of when writing was started.
        /// </summary>
        public long WriteResponse { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var buffer = new StringBuilder();
            AppendTime(buffer.Append("Match: "), this.Start, this.PreRequest);
            AppendTime(buffer.Append(", Before: "), this.PreRequest, this.ProcessRequest);
            AppendTime(buffer.Append(", Process: "), this.ProcessRequest, this.PostRequest);
            AppendTime(buffer.Append(", After: "), this.PostRequest, this.WriteResponse);
            AppendTime(buffer.Append(", Write: "), this.WriteResponse, this.Complete);
            AppendTime(buffer.Append(", Total: "), this.Start, this.Complete);
            return buffer.ToString();
        }

        private static void AppendTime(StringBuilder buffer, long start, long end)
        {
            long delta = end - start;
            if (delta <= 0)
            {
                buffer.Append('0');
            }
            else if (delta < 1000)
            {
                buffer.Append(delta.ToString(NumberFormatInfo.InvariantInfo))
                      .Append("µs");
            }
            else if (delta < 1000 * 1000)
            {
                buffer.Append((delta / 1_000.0).ToString("f1", NumberFormatInfo.InvariantInfo))
                      .Append("ms");
            }
            else
            {
                buffer.Append((delta / 1_000_000.0).ToString("f1", NumberFormatInfo.InvariantInfo))
                      .Append('s');
            }
        }
    }
}
