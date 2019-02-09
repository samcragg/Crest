// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System.Collections.Generic;
    using Crest.Host.Serialization.Internal;

    /// <content>
    /// Contains the nested helper <see cref="Adapter"/> class.
    /// </content>
    internal sealed partial class DeserializeDelegateGenerator
    {
        private static class Adapter
        {
            public static T[] ReadArray<T>(
                IFormatter formatter,
                IReadOnlyList<object> metadata,
                DeserializeInstance readElement)
            {
                ArrayBuffer<T> buffer = default;
                if (formatter.ReadBeginArray(typeof(T)))
                {
                    do
                    {
                        buffer.Add((T)readElement(formatter, metadata));
                    }
                    while (formatter.ReadElementSeparator());
                    formatter.ReadEndArray();
                }

                return buffer.ToArray();
            }
        }
    }
}
