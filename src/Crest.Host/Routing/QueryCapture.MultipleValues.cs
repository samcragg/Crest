// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <content>
    /// Contains the nested <see cref="MultipleValues"/> class.
    /// </content>
    internal partial class QueryCapture
    {
        private sealed class MultipleValues : QueryCapture
        {
            private readonly Type elementType;

            internal MultipleValues(string queryKey, Type elementType, IQueryValueConverter converter)
                : base(queryKey, converter)
            {
                this.elementType = elementType;
            }

            /// <inheritdoc/>
            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                var buffer = new ArrayList();
                foreach (string value in query[this.queryKey])
                {
                    if (this.converter.TryConvertValue(new StringSegment(value), out object result))
                    {
                        buffer.Add(result);
                    }

                    // TODO: Trace we couldn't parse it
                }

                parameters[this.converter.ParameterName] = buffer.ToArray(this.elementType);
            }
        }
    }
}
