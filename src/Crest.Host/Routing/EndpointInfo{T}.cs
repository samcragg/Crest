// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <summary>
    /// Contains information about a reachable endpoint.
    /// </summary>
    /// <typeparam name="T">The type of value to store for the endpoint.</typeparam>
    internal sealed class EndpointInfo<T> : IEquatable<EndpointInfo<T>>
    {
        private readonly uint versionRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointInfo{T}"/> class.
        /// </summary>
        /// <param name="verb">The HTTP verb for the endpoint.</param>
        /// <param name="value">The value to store for the endpoint.</param>
        /// <param name="from">The version the endpoint is applicable from.</param>
        /// <param name="to">The version the endpoint is applicable to.</param>
        public EndpointInfo(string verb, T value, int from, int to)
        {
            this.From = from;
            this.versionRange = (uint)(to - from);
            this.Value = value;
            this.Verb = verb.ToUpperInvariant();
        }

        /// <summary>
        /// Gets the version the endpoint is applicable from.
        /// </summary>
        public int From { get; }

        /// <summary>
        /// Gets the version the endpoint is applicable to.
        /// </summary>
        public int To => this.From + (int)this.versionRange;

        /// <summary>
        /// Gets the value for the endpoint.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the HTTP verb to match against.
        /// </summary>
        public string Verb { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return this.Equals(obj as EndpointInfo<T>);
        }

        /// <inheritdoc />
        public bool Equals(EndpointInfo<T> other)
        {
            if (other is null)
            {
                return false;
            }
            else
            {
                return this.Verb.Equals(other.Verb, StringComparison.Ordinal) &&
                    (this.From <= other.To) &&
                    (this.To >= other.From);
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.Verb.GetHashCode();
        }

        /// <summary>
        /// Determines whether the version is compatible with this endpoint.
        /// </summary>
        /// <param name="version">The version to test.</param>
        /// <returns>
        /// <c>true</c> if the endpoint is applicable for the specified version;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Matches(int version)
        {
            uint delta = (uint)version - (uint)this.From;
            return delta <= this.versionRange;
        }
    }
}
