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
    internal sealed class MetadataBuilder
    {
        private readonly Func<PropertyInfo, object> getPropertyMetadata;
        private readonly Dictionary<MemberInfo, int> indexes = new Dictionary<MemberInfo, int>();
        private readonly List<object> metadata = new List<object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataBuilder"/> class.
        /// </summary>
        /// <param name="metadataMethod">The method to supply the metadata.</param>
        public MetadataBuilder(MethodInfo metadataMethod)
        {
            this.getPropertyMetadata = (Func<PropertyInfo, object>)metadataMethod.CreateDelegate(
                typeof(Func<PropertyInfo, object>));
        }

        /// <summary>
        /// Creates a copy of the metadata added to this instance.
        /// </summary>
        /// <param name="type">The type for the array.</param>
        /// <returns>An array containing the metadata.</returns>
        public Array GetMetadataArray(Type type)
        {
            var value = Array.CreateInstance(type, this.metadata.Count);
            this.metadata.CopyTo((object[])value);
            return value;
        }

        /// <summary>
        /// Gets the index of the metadata in the array, adding it to the array
        /// if it does not currently exist.
        /// </summary>
        /// <param name="property">The property to get the metadata for.</param>
        /// <returns>The index in the metadata array for the property.</returns>
        public int GetOrAddMetadata(PropertyInfo property)
        {
            if (!this.indexes.TryGetValue(property, out int index))
            {
                index = this.metadata.Count;
                this.metadata.Add(this.getPropertyMetadata(property));
                this.indexes.Add(property, index);
            }

            return index;
        }

        /// <summary>
        /// Gets the index of the metadata in the array, adding it to the array
        /// if it does not currently exist.
        /// </summary>
        /// <param name="type">The type to get the metadata for.</param>
        /// <param name="getMetadata">
        /// Used to create the metadata if it does not exist.
        /// </param>
        /// <returns>The index in the metadata array for the type.</returns>
        public int GetOrAddMetadata(Type type, Func<object> getMetadata)
        {
            if (!this.indexes.TryGetValue(type, out int index))
            {
                index = this.metadata.Count;
                this.metadata.Add(getMetadata());
                this.indexes.Add(type, index);
            }

            return index;
        }
    }
}
