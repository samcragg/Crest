// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Provides methods to help build the metadata cache for serialization.
    /// </summary>
    internal partial class MetadataBuilder
    {
        private const string MetadataMethodName = "GetMetadata";
        private const BindingFlags PublicStatic = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;
        private const string TypeMetadataMethodName = "GetTypeMetadata";

        private readonly Dictionary<MemberInfo, int> indexes = new Dictionary<MemberInfo, int>();
        private readonly int offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataBuilder"/> class.
        /// </summary>
        /// <param name="initialCount">
        /// The value to start the index counting from.
        /// </param>
        public MetadataBuilder(int initialCount)
        {
            this.offset = initialCount;
        }

        /// <summary>
        /// Gets the metadata for the members added to this instance.
        /// </summary>
        /// <typeparam name="T">The type to generate the metadata for.</typeparam>
        /// <returns>An array containing the metadata.</returns>
        public virtual IReadOnlyList<object> CreateMetadata<T>()
        {
            object[] array = new object[this.indexes.Count];
            foreach (KeyValuePair<MemberInfo, int> kvp in this.indexes)
            {
                object metadata;
                if (kvp.Key is PropertyInfo property)
                {
                    metadata = MetadataProvider<T>.PropertyMetadataAdapter(property);
                }
                else
                {
                    metadata = MetadataProvider<T>.TypeMetadataAdapter((Type)kvp.Key);
                }

                array[kvp.Value - this.offset] = metadata;
            }

            return array;
        }

        /// <summary>
        /// Gets the index of the metadata in the array, adding it to the array
        /// if it does not currently exist.
        /// </summary>
        /// <param name="member">The member to get the metadata for.</param>
        /// <returns>The index in the metadata array for the member.</returns>
        public virtual int GetOrAddMetadata(MemberInfo member)
        {
            if (!this.indexes.TryGetValue(member, out int index))
            {
                index = this.offset + this.indexes.Count;
                this.indexes.Add(member, index);
            }

            return index;
        }
    }
}
