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
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Crest.Host.Engine;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Base class for the generation of methods for deserializing/serializing
    /// classes at runtime.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    internal abstract partial class DelegateGenerator<TDelegate>
        where TDelegate : Delegate
    {
        private readonly MethodInfo createSerializerFromDelegateMethod =
            typeof(DelegateGenerator<TDelegate>)
            .GetMethod(nameof(CreateSerializerFromDelegate), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Dictionary<Type, TDelegate> delegates = new Dictionary<Type, TDelegate>();
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
        protected IDictionary<Type, TDelegate> KnownDelegates => this.delegates;

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
        public TDelegate CreateDelegate(Type type, MetadataBuilder metadata)
        {
            if (!this.delegates.TryGetValue(type, out TDelegate value))
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
        public bool TryGetDelegate(Type type, out TDelegate method)
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
        protected abstract TDelegate BuildDelegate(Type type, MetadataBuilder metadata);

        /// <summary>
        /// Constructs the custom serializer for the specified type, if any.
        /// </summary>
        /// <param name="type">The type to create the serializer for.</param>
        /// <param name="metadata">Represents the passed in metadata.</param>
        /// <param name="builder">Used to build the metadata for the type.</param>
        /// <returns>
        /// An expression that creates the custom serializer for the type, or
        /// <c>null</c> if the type does not have a custom serializer.
        /// </returns>
        protected Expression CreateCustomSerializer(
            Type type,
            ParameterExpression metadata,
            MetadataBuilder builder)
        {
            Type serializerType = this.GetCustomSerializer(type);
            return (serializerType != null) ?
                this.ConstructSerializer(serializerType, metadata, builder) :
                null;
        }

        /// <summary>
        /// Creates an adapter that is able to read the specified type.
        /// </summary>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <param name="serializer">The generated delegate.</param>
        /// <returns>A wrapper delegate that can read the specified type.</returns>
        /// <remarks>
        /// The returned delegate will be invoked by passing in a IClassReader
        /// to read the class from and the list of metadata. It is expected to
        /// return an instance of the specified type.
        /// </remarks>
        protected virtual Func<IClassReader, IReadOnlyList<object>, T> ReadFromDelegate<T>(TDelegate serializer)
        {
            return (r, m) => throw new NotSupportedException("Cannot read type of " + typeof(T).Name);
        }

        /// <summary>
        /// Creates an adapter that is able to write the specified type.
        /// </summary>
        /// <typeparam name="T">The type to write.</typeparam>
        /// <param name="serializer">The generated delegate.</param>
        /// <returns>A wrapper delegate that can write the specified type.</returns>
        /// <remarks>
        /// The returned delegate will be invoked by passing in a IClassWriter
        /// to write the data, the list of metadata and the instance to write.
        /// </remarks>
        protected virtual Action<IClassWriter, IReadOnlyList<object>, T> WriteToDelegate<T>(TDelegate serializer)
        {
            return (r, m, i) => throw new NotSupportedException("Cannot write type of " + typeof(T).Name);
        }

        private Expression ConstructSerializer(
            Type serializer,
            ParameterExpression metadata,
            MetadataBuilder builder)
        {
            ConstructorInfo[] constructors = serializer.GetConstructors();
            if (constructors.Length > 1)
            {
                throw new InvalidOperationException("Custom serializers must have a single constructor.");
            }

            ParameterInfo[] parameterInfos = constructors[0].GetParameters();
            var arguments = new Expression[parameterInfos.Length];
            foreach (ParameterInfo info in parameterInfos)
            {
                arguments[info.Position] = this.CreateSerializer(info.ParameterType, metadata, builder);
            }

            return Expression.New(constructors[0], arguments);
        }

        private Expression CreateSerializer(
            Type serializer,
            ParameterExpression metadata,
            MetadataBuilder builder)
        {
            if (!serializer.IsGenericType || (serializer.GetGenericTypeDefinition() != typeof(ISerializer<>)))
            {
                throw new InvalidOperationException(
                    "Custom serializers can only have other ISerializer's injected into the constructor");
            }

            Type typeToSerialize = serializer.GetGenericArguments()[0];
            Type customSerializer = this.GetCustomSerializer(typeToSerialize);
            if (customSerializer == null)
            {
                return (Expression)this.createSerializerFromDelegateMethod
                    .MakeGenericMethod(typeToSerialize)
                    .Invoke(this, new object[] { metadata, builder });
            }
            else
            {
                return this.ConstructSerializer(customSerializer, metadata, builder);
            }
        }

        private Expression CreateSerializerFromDelegate<T>(ParameterExpression metadata, MetadataBuilder builder)
        {
            TDelegate serializer = this.CreateDelegate(typeof(T), builder);
            return Expression.New(
                typeof(DelegateSerializerAdapter<T>).GetConstructors().Single(),
                Expression.Constant(this.ReadFromDelegate<T>(serializer)),
                Expression.Constant(this.WriteToDelegate<T>(serializer)),
                metadata);
        }

        private Type GetCustomSerializer(Type type)
        {
            Type serializerInterface = typeof(ISerializer<>).MakeGenericType(type);
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
