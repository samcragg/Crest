// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <summary>
    /// Represents the result of matching part of a URL.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "This is a short lived object that will not be used in comparisons")]
    internal struct NodeMatchInfo
    {
        /// <summary>
        /// Represents a non-successful match.
        /// </summary>
        internal static readonly NodeMatchInfo None = new NodeMatchInfo(-1, null, null);

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeMatchInfo"/> struct.
        /// </summary>
        /// <param name="length">The length of the URL that was matched.</param>
        /// <param name="key">The name of the captured parameter.</param>
        /// <param name="value">The value of the captured parameter.</param>
        public NodeMatchInfo(int length, string key, object value)
        {
            this.MatchLength = length;
            this.Parameter = key;
            this.Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether a parameter has been captured or not.
        /// </summary>
        public bool HasCapture => this.Parameter != null;

        /// <summary>
        /// Gets the length of the URL that was matched.
        /// </summary>
        public int MatchLength { get; }

        /// <summary>
        /// Gets the name of the captured parameter, if any.
        /// </summary>
        public string Parameter { get; }

        /// <summary>
        /// Gets a value indicating whether the segment was matched or not.
        /// </summary>
        public bool Success => this.MatchLength >= 0;

        /// <summary>
        /// Gets the value of the captured parameter, if any.
        /// </summary>
        public object Value { get; }
    }
}
