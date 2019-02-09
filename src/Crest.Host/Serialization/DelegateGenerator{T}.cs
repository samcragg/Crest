// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Crest.Host.Engine;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Base class for the generation of methods for deserializing/serializing
    /// classes at runtime.
    /// </summary>
    /// <typeparam name="T">The type of the delegate.</typeparam>
    internal abstract class DelegateGenerator<T>
        where T : Delegate
    {
        private readonly Dictionary<Type, T> delegates = new Dictionary<Type, T>();
        private readonly DiscoveredTypes discoveredTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateGenerator{T}"/> class.
        /// </summary>
        /// <param name="discoveredTypes">The available types.</param>
        protected DelegateGenerator(DiscoveredTypes discoveredTypes)
        {
            this.discoveredTypes = discoveredTypes;
            this.Methods = new Methods();
        }

        /// <summary>
        /// Gets the generated delegates stored against their type.
        /// </summary>
        protected IDictionary<Type, T> KnownDelegates => this.delegates;

        /// <summary>
        /// Gets the metadata for the methods of internal types.
        /// </summary>
        protected Methods Methods { get; }

        /// <summary>
        /// Creates a delegate for the specified type.
        /// </summary>
        /// <param name="type">The type to generate for.</param>
        /// <param name="metadata">Used to build the metadata for the type.</param>
        /// <returns>A delegate for the specified type.</returns>
        public T CreateDelegate(Type type, MetadataBuilder metadata)
        {
            if (!this.delegates.TryGetValue(type, out T value))
            {
                // Create a marker to avoid circular references with nested
                // serializers (i.e. the next call to this method for the same
                // type will "succeed", however, we'll throw an exception for
                // the null later)
                this.delegates[type] = null;
                value = this.BuildDelegate(type, metadata);
                this.delegates[type] = value;
            }

            return value ?? throw new InvalidOperationException(
                "Cyclic dependency detected for " + type.Name);
        }

        /// <summary>
        /// Generates a method to serialize the specified type.
        /// </summary>
        /// <param name="type">The type information.</param>
        /// <param name="method">
        /// When this method returns, contains the delegate for the type, if
        /// found; otherwise, <c>null</c>.
        /// </param>
        /// <returns>A delegate that can serialize the specified type.</returns>
        public bool TryGetDelegate(Type type, out T method)
        {
            return this.delegates.TryGetValue(type, out method);
        }

        /// <summary>
        /// Determines whether the specified type can contain a <c>null</c> value.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>
        /// <c>true</c> if <c>null</c> is a valid value for the type; otherwise,
        /// <c>false</c>.
        /// </returns>
        protected static bool CanBeNull(Type type)
        {
            return !type.GetTypeInfo().IsValueType ||
                   (Nullable.GetUnderlyingType(type) != null);
        }

        /// <summary>
        /// Gets a generic interface method from a type.
        /// </summary>
        /// <param name="type">The type to get the method from.</param>
        /// <param name="interfaceType">The generic interface type.</param>
        /// <param name="method">The name of the method.</param>
        /// <returns>
        /// The method information if it was found, otherwise, <c>null</c>.
        /// </returns>
        protected static MethodInfo GetInterfaceMethod(Type type, Type interfaceType, string method)
        {
            return type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && (i.GetGenericTypeDefinition() == interfaceType))
                ?.GetMethod(method);
        }

        /// <summary>
        /// Gets the properties for a type that should be serialized.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>The sequence of properties to serialize.</returns>
        protected static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            int DataOrder(PropertyInfo property)
            {
                DataMemberAttribute dataMember = property.GetCustomAttribute<DataMemberAttribute>();
                return (dataMember != null) ? dataMember.Order : int.MaxValue;
            }

            bool IncludeProperty(PropertyInfo property)
            {
                BrowsableAttribute browsable = property.GetCustomAttribute<BrowsableAttribute>();
                if ((browsable != null) && !browsable.Browsable)
                {
                    return false;
                }

                return property.CanRead && property.CanWrite;
            }

            return type.GetProperties()
                       .Where(IncludeProperty)
                       .OrderBy(DataOrder);
        }

        /// <summary>
        /// Builds a delegate for the specified type.
        /// </summary>
        /// <param name="type">The type to build for.</param>
        /// <param name="metadata">Used to build the metadata for the type.</param>
        /// <returns>A delegate for the specified type.</returns>
        protected abstract T BuildDelegate(Type type, MetadataBuilder metadata);

        /// <summary>
        /// Gets the custom serializer for the specified type.
        /// </summary>
        /// <param name="type">The type to get the serializer for.</param>
        /// <returns>
        /// A custom serializer for the type, or <c>null</c> if none were found.
        /// </returns>
        protected Type GetCustomSerializer(Type type)
        {
            Type serializerInterface = typeof(ICustomSerializer<>).MakeGenericType(type);
            IEnumerator<Type> serializerTypes =
                this.discoveredTypes
                    .Types
                    .Where(serializerInterface.IsAssignableFrom)
                    .GetEnumerator();

            Type serializer = null;
            if (serializerTypes.MoveNext())
            {
                serializer = serializerTypes.Current;
                if (serializerTypes.MoveNext())
                {
                    throw new InvalidOperationException("Multiple serializers found for " + type.Name);
                }
            }

            return serializer;
        }
    }
}
