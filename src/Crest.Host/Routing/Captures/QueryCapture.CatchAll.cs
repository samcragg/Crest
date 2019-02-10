// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Conversion;

    /// <content>
    /// Contains the nested <see cref="CatchAll"/> class.
    /// </content>
    internal partial class QueryCapture
    {
        private class CatchAll : QueryCapture
        {
            private readonly string parameterName;

            public CatchAll(string parameterName)
                : base(null, null)
            {
                this.parameterName = parameterName;
            }

            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                parameters.Add(
                    this.parameterName,
                    new DynamicQuery(query, (IReadOnlyDictionary<string, object>)parameters));
            }
        }
    }
}
