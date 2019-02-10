// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core.Logging;

    /// <content>
    /// Contains the nested <see cref="SingleValue"/> class.
    /// </content>
    internal partial class QueryCapture
    {
        private sealed class SingleValue : QueryCapture
        {
            internal SingleValue(string parameterName, IQueryValueConverter converter)
                : base(parameterName, converter)
            {
            }

            /// <inheritdoc/>
            public override void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters)
            {
                foreach (string value in query[this.ParameterName])
                {
                    if (this.converter.TryConvertValue(value.AsSpan(), out object result))
                    {
                        parameters.Add(this.converter.ParameterName, result);
                        break;
                    }

                    Logger.ErrorFormat(
                        "Unable to convert the value '{value}' for parameter '{parameter}'",
                        value,
                        this.converter.ParameterName);
                }
            }
        }
    }
}
