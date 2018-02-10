// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Diagnostics;

    /// <summary>
    /// Represents the result of a parse operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    [DebuggerDisplay("{Value}")]
    internal struct ParseResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseResult{T}"/> structure.
        /// </summary>
        /// <param name="value">The parsed value.</param>
        /// <param name="length">The number of characters parsed.</param>
        public ParseResult(T value, int length)
        {
            this.Error = null;
            this.Length = length;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseResult{T}"/> structure.
        /// </summary>
        /// <param name="error">The parsing error.</param>
        public ParseResult(string error)
        {
            this.Error = error;
            this.Length = 0;
            this.Value = default;
        }

        /// <summary>
        /// Gets the error found during parsing.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Gets a value indicating whether the parsing operation was a success.
        /// </summary>
        public bool IsSuccess => this.Error == null;

        /// <summary>
        /// Gets the number of characters parsed.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the parsed values.
        /// </summary>
        public T Value { get; }
    }
}
