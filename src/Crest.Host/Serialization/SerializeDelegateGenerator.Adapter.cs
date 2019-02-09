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
    internal partial class SerializeDelegateGenerator
    {
        private static class Adapter
        {
            public static void WriteArray<T>(
                IFormatter formatter,
                IReadOnlyList<object> metadata,
                object instance,
                SerializeInstance writeElement)
            {
                var array = (T[])instance;
                formatter.WriteBeginArray(typeof(T), array.Length);

                if (array.Length > 0)
                {
                    if (array[0] == null)
                    {
                        formatter.Writer.WriteNull();
                    }
                    else
                    {
                        writeElement(formatter, metadata, array[0]);
                    }

                    for (int i = 1; i < array.Length; i++)
                    {
                        formatter.WriteElementSeparator();
                        if (array[i] == null)
                        {
                            formatter.Writer.WriteNull();
                        }
                        else
                        {
                            writeElement(formatter, metadata, array[i]);
                        }
                    }
                }

                formatter.WriteEndArray();
            }
        }
    }
}
