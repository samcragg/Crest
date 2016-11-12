// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <summary>
    /// Represents the result of matching a URL segment.
    /// </summary>
    internal struct NodeMatchResult
    {
        /// <summary>
        /// Represents a non-successful match.
        /// </summary>
        internal static readonly NodeMatchResult None = default(NodeMatchResult);

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeMatchResult"/> struct.
        /// </summary>
        /// <param name="name">The name of the captured parameter.</param>
        /// <param name="value">The value of the captured parameter.</param>
        public NodeMatchResult(string name, object value)
        {
            this.Success = true;
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets the name of the captured parameter, if any.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the segment was matched or not.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the value of the captured parameter, if any.
        /// </summary>
        public object Value { get; }
    }
}