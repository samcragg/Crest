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
            public CatchAll(string parameterName)
                : base(parameterName, null)
            {
            }

            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                parameters.Add(
                    this.ParameterName,
                    new DynamicQuery(query, (IReadOnlyDictionary<string, object>)parameters));
            }
        }
    }
}
