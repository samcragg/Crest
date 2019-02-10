// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core.Logging;

    /// <content>
    /// Contains the nested <see cref="MultipleValues"/> class.
    /// </content>
    internal partial class QueryCapture
    {
        private sealed class MultipleValues : QueryCapture
        {
            private readonly Type elementType;

            internal MultipleValues(string parameterName, Type elementType, IQueryValueConverter converter)
                : base(parameterName, converter)
            {
                this.elementType = elementType;
            }

            /// <inheritdoc/>
            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                var buffer = new ArrayList();
                foreach (string value in query[this.ParameterName])
                {
                    if (this.converter.TryConvertValue(value.AsSpan(), out object result))
                    {
                        buffer.Add(result);
                    }
                    else
                    {
                        Logger.ErrorFormat(
                            "Unable to convert '{value}' to type '{type}'",
                            value,
                            this.elementType);

                        Logger.WarnFormat(
                            "Parameter '{parameter}' does not contain all the information passed in the query due to invalid values",
                            this.converter.ParameterName);
                    }
                }

                parameters.Add(this.converter.ParameterName, buffer.ToArray(this.elementType));
            }
        }
    }
}
