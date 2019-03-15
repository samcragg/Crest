// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Serialization.Internal;

    /// <content>
    /// Contains the nested helper <see cref="DelegateSerializerAdapter{T}"/> class.
    /// </content>
    internal abstract partial class DelegateGenerator<TDelegate>
        where TDelegate : Delegate
    {
        private class DelegateSerializerAdapter<T> : ISerializer<T>
        {
            private readonly IReadOnlyList<object> metadata;
            private readonly Func<IClassReader, IReadOnlyList<object>, T> read;
            private readonly Action<IClassWriter, IReadOnlyList<object>, T> write;

            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Major Code Smell",
                "S1144:Unused private types or members should be removed",
                Justification = "Accessed by runtime generated code via Linq.Expressions")]
            public DelegateSerializerAdapter(
                Func<IClassReader, IReadOnlyList<object>, T> read,
                Action<IClassWriter, IReadOnlyList<object>, T> write,
                IReadOnlyList<object> metadata)
            {
                this.metadata = metadata;
                this.read = read;
                this.write = write;
            }

            public T Read(IClassReader reader)
            {
                return this.read(reader, this.metadata);
            }

            public void Write(IClassWriter writer, T instance)
            {
                this.write(writer, this.metadata, instance);
            }
        }
    }
}
