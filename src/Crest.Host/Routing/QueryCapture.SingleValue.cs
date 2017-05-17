// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Diagnostics;

    /// <content>
    /// Contains the nested <see cref="SingleValue"/> class.
    /// </content>
    internal partial class QueryCapture
    {
        private sealed class SingleValue : QueryCapture
        {
            internal SingleValue(string queryKey, IQueryValueConverter converter)
                : base(queryKey, converter)
            {
            }

            /// <inheritdoc/>
            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                foreach (string value in query[this.queryKey])
                {
                    if (this.converter.TryConvertValue(new StringSegment(value), out object result))
                    {
                        parameters[this.converter.ParameterName] = result;
                        break;
                    }

                    TraceSources.Routing.TraceError(
                        "Unable to convert the value '{0}' for parameter '{1}'",
                        value,
                        this.converter.ParameterName);
                }
            }
        }
    }
}
