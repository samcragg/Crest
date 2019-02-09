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
        /// Gets the methods for the <see cref="Serialization.CaseInsensitiveStringHelper"/> class.
        /// </summary>
        internal CaseInsensitiveStringHelperMethods CaseInsensitiveStringHelper { get; }
            = new CaseInsensitiveStringHelperMethods();

        /// <summary>
        /// Gets the methods for the <see cref="IClassReader"/> interface.
        /// </summary>
        internal ClassReaderMethods ClassReader { get; } = new ClassReaderMethods();

        /// <summary>
        /// Gets the methods for the <see cref="IClassWriter"/> interface.
        /// </summary>
        internal ClassWriterMethods ClassWriter { get; } = new ClassWriterMethods();

        /// <summary>
        /// Gets the methods for the <see cref="System.Enum"/> class.
        /// </summary>
        internal EnumMethods Enum { get; } = new EnumMethods();

        /// <summary>
        /// Gets the methods for the <see cref="IFormatter"/> interface.
        /// </summary>
        internal FormatterMethods Formatter { get; } = new FormatterMethods();

        /// <summary>
        /// Gets the methods for the <see cref="object"/> class.
        /// </summary>
        internal ObjectMethods Object { get; } = new ObjectMethods();

        /// <summary>
        /// Gets the methods for the <see cref="IReadOnlyList{T}"/> class.
        /// </summary>
        internal ReadOnlyListMethods ReadOnlyList { get; } = new ReadOnlyListMethods();

        /// <summary>
        /// Gets the methods for the <see cref="Internal.ValueReader"/> class.
        /// </summary>
        internal ValueReaderMethods ValueReader { get; } = new ValueReaderMethods();

        /// <summary>
        /// Gets the methods for the <see cref="Internal.ValueWriter"/> interface.
        /// </summary>
        internal ValueWriterMethods ValueWriter { get; } = new ValueWriterMethods();

        /// <summary>
        /// Contains the methods of the <see cref="Serialization.CaseInsensitiveStringHelper"/> class.
        /// </summary>
        internal sealed class CaseInsensitiveStringHelperMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CaseInsensitiveStringHelperMethods"/> class.
            /// </summary>
            public CaseInsensitiveStringHelperMethods()
            {
                this.Equals = typeof(CaseInsensitiveStringHelper).GetMethod(
                    nameof(Serialization.CaseInsensitiveStringHelper.Equals),
                    new[] { typeof(string), typeof(string) });

                this.GetHashCode = typeof(CaseInsensitiveStringHelper).GetMethod(
                    nameof(Serialization.CaseInsensitiveStringHelper.GetHashCode),
                    new[] { typeof(string) });
            }

            /// <summary>
            /// Gets the metadata for the <see cref="CaseInsensitiveStringHelper.Equals(string, string)"/> method.
            /// </summary>
            public new MethodInfo Equals { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="CaseInsensitiveStringHelper.GetHashCode(string)"/> method.
            /// </summary>
            public new MethodInfo GetHashCode { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="IClassReader"/> interface.
        /// </summary>
        internal class ClassReaderMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ClassReaderMethods"/> class.
            /// </summary>
            public ClassReaderMethods()
            {
                this.GetReader = typeof(IClassReader)
                    .GetProperty(nameof(IClassReader.Reader))
                    .GetGetMethod();

                this.ReadBeginArray = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadBeginArray));

                this.ReadBeginProperty = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadBeginProperty));

                this.ReadElementSeparator = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadElementSeparator));

                this.ReadEndArray = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadEndArray));

                this.ReadEndClass = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadEndClass));

                this.ReadEndProperty = typeof(IClassReader)
                    .GetMethod(nameof(IClassReader.ReadEndProperty));
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.Reader"/> property.
            /// </summary>
            public MethodInfo GetReader { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadBeginArray(Type)"/> method.
            /// </summary>
            public MethodInfo ReadBeginArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadBeginProperty"/> method.
            /// </summary>
            public MethodInfo ReadBeginProperty { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadElementSeparator"/> method.
            /// </summary>
            public MethodInfo ReadElementSeparator { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadEndArray"/> method.
            /// </summary>
            public MethodInfo ReadEndArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadEndClass"/> method.
            /// </summary>
            public MethodInfo ReadEndClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassReader.ReadEndProperty"/> method.
            /// </summary>
            public MethodInfo ReadEndProperty { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="IClassWriter"/> interface.
        /// </summary>
        internal class ClassWriterMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ClassWriterMethods"/> class.
            /// </summary>
            public ClassWriterMethods()
            {
                this.GetWriter = typeof(IClassWriter)
                    .GetProperty(nameof(IClassWriter.Writer))
                    .GetGetMethod();

                this.WriteBeginArray = typeof(IClassWriter)
                    .GetMethod(nameof(IClassWriter.WriteBeginArray));

                this.WriteElementSeparator = typeof(IClassWriter)
                    .GetMethod(nameof(IClassWriter.WriteElementSeparator));

                this.WriteEndArray = typeof(IClassWriter)
                    .GetMethod(nameof(IClassWriter.WriteEndArray));

                this.WriteEndClass = typeof(IClassWriter)
                    .GetMethod(nameof(IClassWriter.WriteEndClass));

                this.WriteEndProperty = typeof(IClassWriter)
                    .GetMethod(nameof(IClassWriter.WriteEndProperty));
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.Writer"/> property.
            /// </summary>
            public MethodInfo GetWriter { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.WriteBeginArray(Type, int)"/> method.
            /// </summary>
            public MethodInfo WriteBeginArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.WriteElementSeparator"/> method.
            /// </summary>
            public MethodInfo WriteElementSeparator { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.WriteEndArray"/> method.
            /// </summary>
            public MethodInfo WriteEndArray { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.WriteEndClass"/> method.
            /// </summary>
            public MethodInfo WriteEndClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IClassWriter.WriteEndProperty"/> method.
            /// </summary>
            public MethodInfo WriteEndProperty { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="System.Enum"/> class.
        /// </summary>
        internal sealed class EnumMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EnumMethods"/> class.
            /// </summary>
            public EnumMethods()
            {
                this.Parse = typeof(Enum)
                    .GetMethod(nameof(System.Enum.Parse), new[] { typeof(Type), typeof(string), typeof(bool) });
            }

            /// <summary>
            /// Gets the metadata for the <see cref="Enum.Parse(Type, string, bool)"/> method.
            /// </summary>
            public MethodInfo Parse { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="IFormatter"/> interface.
        /// </summary>
        internal sealed class FormatterMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FormatterMethods"/> class.
            /// </summary>
            public FormatterMethods()
            {
                this.ReadBeginClass = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.ReadBeginClass));

                this.ReadBeginPrimitive = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.ReadBeginPrimitive));

                this.ReadEndPrimitive = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.ReadEndPrimitive));

                this.WriteBeginClass = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.WriteBeginClass));

                this.WriteBeginPrimitive = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.WriteBeginPrimitive));

                this.WriteEndPrimitive = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.WriteEndPrimitive));

                this.WriteBeginProperty = typeof(IFormatter)
                    .GetMethod(nameof(IFormatter.WriteBeginProperty));

                this.EnumsAsIntegers = typeof(IFormatter)
                    .GetProperty(nameof(IFormatter.EnumsAsIntegers))
                    .GetGetMethod();
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.EnumsAsIntegers"/> property.
            /// </summary>
            public MethodInfo EnumsAsIntegers { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.ReadBeginClass(object)"/> method.
            /// </summary>
            public MethodInfo ReadBeginClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.ReadBeginPrimitive(object)"/> method.
            /// </summary>
            public MethodInfo ReadBeginPrimitive { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.ReadEndPrimitive"/> method.
            /// </summary>
            public MethodInfo ReadEndPrimitive { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.WriteBeginClass(object)"/> method.
            /// </summary>
            public MethodInfo WriteBeginClass { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.WriteBeginPrimitive(object)"/> method.
            /// </summary>
            public MethodInfo WriteBeginPrimitive { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.WriteEndPrimitive"/> method.
            /// </summary>
            public MethodInfo WriteEndPrimitive { get; }

            /// <summary>
            /// Gets the metadata for the <see cref="IFormatter.WriteBeginProperty(object)"/> method.
            /// </summary>
            public MethodInfo WriteBeginProperty { get; }
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
        /// Contains the methods of the <see cref="IReadOnlyList{T}"/> class.
        /// </summary>
        internal class ReadOnlyListMethods
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReadOnlyListMethods"/> class.
            /// </summary>
            public ReadOnlyListMethods()
            {
                this.Item = typeof(IReadOnlyList<object>)
                    .GetProperty("Item");
            }

            /// <summary>
            /// Gets the metadata for the <see cref="IReadOnlyList{T}.this[int]"/> property.
            /// </summary>
            public PropertyInfo Item { get; }
        }

        /// <summary>
        /// Contains the methods of the <see cref="Internal.ValueReader"/> class.
        /// </summary>
        internal sealed class ValueReaderMethods
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
