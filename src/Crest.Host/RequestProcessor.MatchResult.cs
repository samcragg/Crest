// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <content>
    /// Contains the nested helper <see cref="MatchResult"/> struct.
    /// </content>
    public abstract partial class RequestProcessor
    {
        /// <summary>
        /// Gets the result of calling <see cref="Match(string, string, ILookup{string, string})"/>
        /// </summary>
        protected internal struct MatchResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MatchResult"/> struct.
            /// </summary>
            /// <param name="method">The matched method.</param>
            /// <param name="parameters">The parsed parameters for the method.</param>
            internal MatchResult(MethodInfo method, IReadOnlyDictionary<string, object> parameters)
            {
                this.Method = method;
                this.Parameters = parameters;
            }

            /// <summary>
            /// Gets the matched method, if any.
            /// </summary>
            public MethodInfo Method { get; }

            /// <summary>
            /// Gets the matched parameters for the matched method.
            /// </summary>
            /// <remarks>
            /// This parameter will be <c>null</c> if no method was found.
            /// </remarks>
            public IReadOnlyDictionary<string, object> Parameters { get; }

            /// <summary>
            /// Gets a value indicating whether a match was found.
            /// </summary>
            public bool Success
            {
                get { return this.Method != null; }
            }
        }
    }
}
