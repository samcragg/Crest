// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Helper class that contains the method metadata called by the generated
    /// serializers.
    /// </summary>
    internal sealed class Methods
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Methods"/> class.
        /// </summary>
        /// <param name="baseClass">
        /// The type the generated classes will inherit from.
        /// </param>
        public Methods(Type baseClass)
        {
            this.BaseClass = new BaseClassMethods(baseClass);
            this.PrimitiveSerializer = new PrimitiveSerializerMethods(baseClass);
        }

        /// <summary>
        /// Gets the methods for the <see cref="IArraySerializer"/> interface.
        /// </summary>
        internal ArraySerializerMethods ArraySerializer { get; } = new ArraySerializerMethods();

        /// <summary>
        /// Gets the methods for the base class.
        /// </summary>
        internal BaseClassMethods BaseClass { get; }

        /// <summary>
        /// Gets the methods for the <see cref="object"/> class.
        /// </summary>
        internal ObjectMethods Object { get; } = new ObjectMethods();

        /// <summary>
        /// Gets the methods for the <see cref="IPrimitiveSerializer{T}"/> interface.
        /// </summary>
        internal PrimitiveSerializerMethods PrimitiveSerializer { get; }

        /// <summary>
        /// Gets the methods for the <see cref="Internal.ValueReader"/> class.
        /// </summary>
        internal ValueReaderMethods ValueReader { get; } = new ValueReaderMethods();

        /// <summary>
        /// Gets the methods for the <see cref="Internal.ValueWriter"/> interface.
        /// </summary>
        internal ValueWriterMethods ValueWriter { get; } = new ValueWriterMethods();

        /// <summary>
        /// Contains the methods of the <see cref="IArraySerializer"/> interface.
        /// </summary>
        internal class ArraySerializerMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ArraySerializerMethods"/> class.
            /// </summary>
            public ArraySerializerMethods()
            {
                this.ReadBeginArray = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.ReadBeginArray));

                this.ReadElementSeparator = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.ReadElementSeparator));

                this.ReadEndArray = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.ReadEndArray));

                this.WriteBeginArray = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.WriteBeginArray));

                this.WriteElementSeparator = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.WriteElementSeparator));

                this.WriteEndArray = typeof(IArraySerializer)
                    .GetMethod(nameof(IArraySerializer.WriteEndArray));
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.ReadBeginArray(Type)"/> method.
            /// </summary>
            public MethodInfo ReadBeginArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.ReadElementSeparator"/> method.
            /// </summary>
            public MethodInfo ReadElementSeparator { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.ReadEndArray"/> method.
            /// </summary>
            public MethodInfo ReadEndArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.WriteBeginArray(Type, int)"/> method.
            /// </summary>
            public MethodInfo WriteBeginArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.WriteElementSeparator"/> method.
            /// </summary>
            public MethodInfo WriteElementSeparator { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.WriteEndArray"/> method.
            /// </summary>
            public MethodInfo WriteEndArray { get; }
        }

        /// <summary>
        /// Contains the methods of the base class.
        /// </summary>
        internal class BaseClassMethods
        {
            private const string MetadataMethodName = "GetMetadata";
            private const string TypeMetadataMethodName = "GetTypeMetadata";

            /// <summary>
            /// Initializes a new instance of the <see cref="BaseClassMethods"/> class.
            /// </summary>
            /// <param name="baseClass">
            /// The type the generated classes will inherit from.
            /// </param>
            public BaseClassMethods(Type baseClass)
            {
                Type classSerializerInterface = TypeSerializerGenerator.GetGenericInterfaceImplementation(
                    baseClass.GetTypeInfo(),
                    typeof(IClassSerializer<>));

                Type primitiveSerializerInterface = TypeSerializerGenerator.GetGenericInterfaceImplementation(
                    baseClass.GetTypeInfo(),
                    typeof(IPrimitiveSerializer<>));

                const BindingFlags PublicStatic = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;

                this.GetMetadata =
                    baseClass.GetMethod(MetadataMethodName, PublicStatic)
                    ?? throw new InvalidOperationException(baseClass.Name + " must contain a public static method called " + MetadataMethodName);

                // Can be null as it's optional
                this.GetTypeMetadata = baseClass.GetMethod(
                    TypeMetadataMethodName,
                    BindingFlags.Public | BindingFlags.Static);

                this.GetWriter = primitiveSerializerInterface
                    .GetProperty(nameof(IPrimitiveSerializer<object>.Writer))
                    .GetGetMethod();

                this.WriteBeginClass = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteBeginClass));

                this.WriteBeginProperty = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteBeginProperty));

                this.WriteEndClass = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteEndClass));

                this.WriteEndProperty = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteEndProperty));
            }

            /// <summary>
            /// Gets the metadata for the <c>GetMetadata</c> method.
            /// </summary>
            public MethodInfo GetMetadata { get; }

            /// <summary>
            /// Gets the metadata for the optional <c>GetTypeMetadata</c> method.
            /// </summary>
            public MethodInfo GetTypeMetadata { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IPrimitiveSerializer{T}.Writer"/> property.
            /// </summary>
            public MethodInfo GetWriter { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassSerializer{T}.WriteBeginClass(T)"/> method.
            /// </summary>
            public MethodInfo WriteBeginClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassSerializer{T}.WriteBeginProperty(T)"/> method.
            /// </summary>
            public MethodInfo WriteBeginProperty { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassSerializer{T}.WriteEndClass"/> method.
            /// </summary>
            public MethodInfo WriteEndClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassSerializer{T}.WriteEndProperty"/> method.
            /// </summary>
            public MethodInfo WriteEndProperty { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="object"/> class.
        /// </summary>
        internal class ObjectMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ObjectMethods"/> class.
            /// </summary>
            public ObjectMethods()
            {
                this.ToString = typeof(object)
                    .GetMethod(nameof(object.ToString));
            }

            /// <summary>
            /// Gets the metadata for the <see cref="object.ToString"/> method.
            /// </summary>
            public new MethodInfo ToString { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="IPrimitiveSerializer{T}"/> interface.
        /// </summary>
        internal class PrimitiveSerializerMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PrimitiveSerializerMethods"/> class.
            /// </summary>
            /// <param name="baseClass">
            /// The type the generated classes will inherit from.
            /// </param>
            public PrimitiveSerializerMethods(Type baseClass)
            {
                Type primitiveSerializer = TypeSerializerGenerator.GetGenericInterfaceImplementation(
                    baseClass.GetTypeInfo(),
                    typeof(IPrimitiveSerializer<>));

                this.BeginWrite = primitiveSerializer
                    .GetMethod(nameof(IPrimitiveSerializer<object>.BeginWrite));

                this.EndWrite = primitiveSerializer
                    .GetMethod(nameof(IPrimitiveSerializer<object>.EndWrite));

                this.GetReader = typeof(IArraySerializer)
                    .GetProperty(nameof(IArraySerializer.Reader))
                    .GetGetMethod();

                this.GetWriter = primitiveSerializer
                    .GetProperty(nameof(IPrimitiveSerializer<object>.Writer))
                    .GetGetMethod();
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IPrimitiveSerializer{T}.BeginWrite(T)"/> method.
            /// </summary>
            public MethodInfo BeginWrite { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IPrimitiveSerializer{T}.EndWrite"/> method.
            /// </summary>
            public MethodInfo EndWrite { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IArraySerializer.Reader"/> property.
            /// </summary>
            public MethodInfo GetReader { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IPrimitiveSerializer{T}.Writer"/> property.
            /// </summary>
            public MethodInfo GetWriter { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="Internal.ValueReader"/> class.
        /// </summary>
        internal sealed class ValueReaderMethods : IEnumerable<KeyValuePair<Type, MethodInfo>>
        {
            private readonly Dictionary<Type, MethodInfo> methods;

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueReaderMethods"/> class.
            /// </summary>
            public ValueReaderMethods()
            {
                this.ReadNull = typeof(ValueReader).GetMethod(
                    nameof(Internal.ValueReader.ReadNull));

                this.ReadObject = typeof(ValueReader).GetMethod(
                    nameof(Internal.ValueReader.ReadObject));

                this.methods = typeof(ValueReader).GetTypeInfo()
                    .DeclaredMethods
                    .Where(m => m.IsPublic)
                    .Where(m => (m != this.ReadNull) && (m != this.ReadObject))
                    .Where(m => m.Name.StartsWith("Read", StringComparison.Ordinal))
                    .ToDictionary(m => m.ReturnType);
            }

            /// <summary>
            /// Gets the metadata for the <see cref="ValueReader.ReadNull"/> method.
            /// </summary>
            public MethodInfo ReadNull { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="ValueReader.ReadObject(Type)"/> method.
            /// </summary>
            public MethodInfo ReadObject { get; }

            /// <summary>
            /// Gets the write method that accepts the specified type as an argument.
            /// </summary>
            /// <param name="type">The type of the argument.</param>
            /// <returns>The metadata for the method.</returns>
            public MethodInfo this[Type type] => this.methods[type];

            /// <inheritdoc />
            public IEnumerator<KeyValuePair<Type, MethodInfo>> GetEnumerator()
            {
                return this.methods.GetEnumerator();
            }

            /// <summary>
            /// Gets the write method that accepts the specified type as an argument.
            /// </summary>
            /// <param name="type">The type of the argument.</param>
            /// <param name="method">
            /// When this methods returns, contains the metadata for the method
            /// or <c>null</c> if no method was found.
            /// </param>
            /// <returns>
            /// <c>true</c> if a write method was found that accepts the
            /// specified type as an argument; otherwise, <c>false</c>.
            /// </returns>
            public bool TryGetMethod(Type type, out MethodInfo method)
            {
                return this.methods.TryGetValue(type, out method);
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Contains the methods of the <see cref="Internal.ValueWriter"/> interface.
        /// </summary>
        internal class ValueWriterMethods : IEnumerable<KeyValuePair<Type, MethodInfo>>
        {
            private readonly Dictionary<Type, MethodInfo> methods;

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueWriterMethods"/> class.
            /// </summary>
            public ValueWriterMethods()
            {
                this.WriteNull = typeof(ValueWriter).GetMethod(
                    nameof(Internal.ValueWriter.WriteNull));

                this.WriteObject = typeof(ValueWriter).GetMethod(
                    nameof(Internal.ValueWriter.WriteObject));

                this.methods = typeof(ValueWriter).GetTypeInfo()
                    .DeclaredMethods
                    .Where(m => m.Name.StartsWith("Write", StringComparison.Ordinal))
                    .Where(m => (m != this.WriteNull) && (m != this.WriteObject))
                    .ToDictionary(m => m.GetParameters().Single().ParameterType);
            }

            /// <summary>
            /// Gets the metadata for the <see cref="ValueWriter.WriteNull"/> method.
            /// </summary>
            public MethodInfo WriteNull { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="ValueWriter.WriteObject(object)"/> method.
            /// </summary>
            public MethodInfo WriteObject { get; }

            /// <summary>
            /// Gets the write method that accepts the specified type as an argument.
            /// </summary>
            /// <param name="type">The type of the argument.</param>
            /// <returns>The metadata for the method.</returns>
            public MethodInfo this[Type type] => this.methods[type];

            /// <inheritdoc />
            public IEnumerator<KeyValuePair<Type, MethodInfo>> GetEnumerator()
            {
                return this.methods.GetEnumerator();
            }

            /// <summary>
            /// Gets the write method that accepts the specified type as an argument.
            /// </summary>
            /// <param name="type">The type of the argument.</param>
            /// <param name="method">
            /// When this methods returns, contains the metadata for the method
            /// or <c>null</c> if no method was found.
            /// </param>
            /// <returns>
            /// <c>true</c> if a write method was found that accepts the
            /// specified type as an argument; otherwise, <c>false</c>.
            /// </returns>
            public bool TryGetMethod(Type type, out MethodInfo method)
            {
                return this.methods.TryGetValue(type, out method);
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
