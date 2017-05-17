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
    using Crest.Host.Diagnostics;

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
                    else
                    {
                        TraceSources.Routing.TraceError(
                            "Unable to convert '{0}' to type '{1}'",
                            value,
                            this.elementType);

                        TraceSources.Routing.TraceWarning(
                            "Parameter '{0}' does not contain all the information passed in the query dues to invalid values",
                            this.converter.ParameterName);
                    }
                }

                parameters[this.converter.ParameterName] = buffer.ToArray(this.elementType);
            }
        }
    }
}
