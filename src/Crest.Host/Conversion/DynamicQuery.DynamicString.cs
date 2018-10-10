// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;

    /// <content>
    /// Contains the nested <see cref="DynamicString"/> interface.
    /// </content>
    internal sealed partial class DynamicQuery
    {
        private class DynamicString : DynamicObject
        {
            private readonly string[] values;

            internal DynamicString(string[] values)
            {
                this.values = values;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                if (IsAssignableFromArray(binder.ReturnType))
                {
                    result = this.values;
                }
                else if (binder.ReturnType == typeof(string))
                {
                    result = this.values.FirstOrDefault();
                }
                else
                {
                    result = Convert.ChangeType(
                        this.values.FirstOrDefault(),
                        binder.ReturnType,
                        CultureInfo.InvariantCulture);
                }

                return true;
            }

            private static bool IsAssignableFromArray(Type type)
            {
                return
                    type == typeof(string[]) ||
                    type == typeof(IEnumerable<string>) ||
                    type == typeof(IReadOnlyCollection<string>) ||
                    type == typeof(IReadOnlyList<string>) ||
                    type == typeof(ICollection<string>) ||
                    type == typeof(IList<string>);
            }
        }
    }
}
