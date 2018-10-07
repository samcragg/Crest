// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;

    /// <content>
    /// Contains the nested helper <see cref="MatchResult"/> struct.
    /// </content>
    public abstract partial class RequestProcessor
    {
        /// <summary>
        /// Gets the result of calling <see cref="Match(string, string, ILookup{string, string})"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1815:Override equals and operator equals on value types",
            Justification = "This is a short lived object that will not be used in comparisons")]
        protected internal struct MatchResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MatchResult"/> struct.
            /// </summary>
            /// <param name="overrideFunc">
            /// The function to invoke to get the response.
            /// </param>
            public MatchResult(OverrideMethod overrideFunc)
            {
                this.Method = null;
                this.Override = overrideFunc;
                this.Parameters = null;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MatchResult"/> struct.
            /// </summary>
            /// <param name="method">The matched method.</param>
            /// <param name="parameters">The parsed parameters for the method.</param>
            public MatchResult(MethodInfo method, IReadOnlyDictionary<string, object> parameters)
            {
                Check.IsNotNull(method, nameof(method));
                Check.IsNotNull(parameters, nameof(parameters));

                this.Method = method;
                this.Override = null;
                this.Parameters = parameters;
            }

            /// <summary>
            /// Gets a value indicating whether to invoke the <see cref="Override"/>
            /// or, if false, process the <see cref="Method"/>.
            /// </summary>
            public bool IsOverride => this.Override != null;

            /// <summary>
            /// Gets the matched method, if any.
            /// </summary>
            public MethodInfo Method { get; }

            /// <summary>
            /// Gets the delegate to invoke to get the response directly without
            /// normal processing, if any.
            /// </summary>
            public OverrideMethod Override { get; }

            /// <summary>
            /// Gets the matched parameters for the matched method.
            /// </summary>
            /// <remarks>
            /// This parameter will be <c>null</c> if no method was found.
            /// </remarks>
            public IReadOnlyDictionary<string, object> Parameters { get; }

            //// <summary>
            //// Gets a value indicating whether this instance has been
            //// constructed with one of the constructors or not.
            //// </summary>
            ////internal bool IsValid => (this.Method != null) || (this.Override != null);
        }
    }
}
