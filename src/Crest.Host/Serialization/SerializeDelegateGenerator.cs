// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Host.Engine;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Serializes a class to the formatter.
    /// </summary>
    /// <param name="formatter">Used to write the data stream.</param>
    /// <param name="metadata">Contains the pre-generated metadata.</param>
    /// <param name="instance">The instance to serialize.</param>
    internal delegate void SerializeInstance(IFormatter formatter, IReadOnlyList<object> metadata, object instance);

    /// <summary>
    /// Generates methods for serializing a class.
    /// </summary>
    internal sealed partial class SerializeDelegateGenerator : DelegateGenerator<SerializeInstance>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeDelegateGenerator"/> class.
        /// </summary>
        /// <param name="discoveredTypes">The available types.</param>
        /// <param name="metadataBuilder">Used to build the metadata for the type.</param>
        public SerializeDelegateGenerator(
            DiscoveredTypes discoveredTypes,
            MetadataBuilder metadataBuilder)
            : base(discoveredTypes)
        {
            AddPrimitiveDelegates(this.KnownDelegates, metadataBuilder.GetOrAddMetadata);
        }

        /// <inheritdoc />
        protected override SerializeInstance BuildDelegate(Type type, MetadataBuilder metadata)
        {
            var builder = new DelegateBuilder(type, metadata);

            if (type.IsArray)
            {
                this.WriteArray(type, builder);
            }
            else if (type.IsValueType)
            {
                this.WriteValueType(type, builder);
            }
            else if (!this.WriteToCustomSerializer(type, builder))
            {
                this.WriteClass(type, builder);
            }

            return Expression.Lambda<SerializeInstance>(
                builder.BuildBody(),
                new[] { builder.Formatter, builder.Metadata, builder.RawInstance })
                .Compile();
        }

        /// <inheritdoc />
        protected override Action<IClassWriter, IReadOnlyList<object>, T> WriteToDelegate<T>(SerializeInstance serializer)
        {
            return (w, m, i) => serializer((IFormatter)w, m, i);
        }

        private Expression CallWriteEnumExpression(DelegateBuilder builder, Expression value, Type valueType)
        {
            Type underlyingType = valueType.GetEnumUnderlyingType();
            return Expression.IfThenElse(
                Expression.Property(builder.Formatter, this.Methods.Formatter.EnumsAsIntegers),
                Expression.Call(
                    Expression.Property(builder.Formatter, this.Methods.ClassWriter.GetWriter),
                    this.Methods.ValueWriter[underlyingType],
                    Expression.Convert(value, underlyingType)),
                Expression.Call(
                    Expression.Property(builder.Formatter, this.Methods.ClassWriter.GetWriter),
                    this.Methods.ValueWriter[typeof(string)],
                    Expression.Call(value, this.Methods.Object.ToString)));
        }

        private Expression CallWriterMethodExpression(DelegateBuilder builder, Expression value, Type valueType)
        {
            if (valueType.IsEnum)
            {
                return this.CallWriteEnumExpression(builder, value, valueType);
            }
            else if (this.Methods.ValueWriter.TryGetMethod(valueType, out MethodInfo writeMethod))
            {
                return Expression.Call(
                    Expression.Property(builder.Formatter, this.Methods.ClassWriter.GetWriter),
                    writeMethod,
                    value);
            }
            else
            {
                return Expression.Call(
                    Expression.Property(builder.Formatter, this.Methods.ClassWriter.GetWriter),
                    this.Methods.ValueWriter.WriteObject,
                    Expression.Convert(value, typeof(object)));
            }
        }

        private void WriteArray(Type type, DelegateBuilder builder)
        {
            Type elementType = type.GetElementType();
            SerializeInstance writeElement = this.CreateDelegate(elementType, builder.MetadataBuilder);

            MethodInfo writeArrayMethod = typeof(Adapter)
                .GetMethod(nameof(Adapter.WriteArray))
                .MakeGenericMethod(elementType);

            builder.Add(Expression.Call(
                writeArrayMethod,
                builder.Formatter,
                builder.Metadata,
                builder.RawInstance,
                Expression.Constant(writeElement)));
        }

        private void WriteClass(Type type, DelegateBuilder builder)
        {
            int index = builder.MetadataBuilder.GetOrAddMetadata(type);
            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.Formatter.WriteBeginClass,
                Expression.MakeIndex(
                    builder.Metadata,
                    this.Methods.ReadOnlyList.Item,
                    new[] { Expression.Constant(index) })));

            this.WriteProperties(type, builder);

            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.ClassWriter.WriteEndClass));
        }

        private void WriteProperties(Type type, DelegateBuilder builder)
        {
            foreach (PropertyInfo property in GetProperties(type))
            {
                MemberExpression accessProperty = Expression.Property(builder.TypedInstance, property);
                Expression writeProperty = this.WritePropertyExpression(builder, property, accessProperty);

                if (CanBeNull(property.PropertyType))
                {
                    builder.Add(Expression.IfThen(
                        Expression.NotEqual(accessProperty, Expression.Constant(null)),
                        writeProperty));
                }
                else
                {
                    builder.Add(writeProperty);
                }
            }
        }

        private Expression WritePropertyExpression(
            DelegateBuilder builder,
            PropertyInfo property,
            MemberExpression propertyAccess)
        {
            int index = builder.MetadataBuilder.GetOrAddMetadata(property);

            return Expression.Block(
                Expression.Call(
                    builder.Formatter,
                    this.Methods.Formatter.WriteBeginProperty,
                    Expression.MakeIndex(
                        builder.Metadata,
                        this.Methods.ReadOnlyList.Item,
                        new[] { Expression.Constant(index) })),
                this.WriteValueExpression(builder, propertyAccess),
                Expression.Call(
                    builder.Formatter,
                    this.Methods.ClassWriter.WriteEndProperty));
        }

        private Expression WriteSimpleTypeExpression(DelegateBuilder builder, Expression value)
        {
            Type valueType = value.Type;
            Type underlyingType = Nullable.GetUnderlyingType(valueType);
            if (underlyingType != null)
            {
                MethodInfo getValueOrDefault = valueType.GetMethod(
                    nameof(Nullable<int>.GetValueOrDefault),
                    Type.EmptyTypes);

                valueType = underlyingType;
                value = Expression.Call(value, getValueOrDefault);
            }

            return this.CallWriterMethodExpression(builder, value, valueType);
        }

        private bool WriteToCustomSerializer(Type type, DelegateBuilder builder)
        {
            Expression createSerializer = this.CreateCustomSerializer(
                type,
                builder.Metadata,
                builder.MetadataBuilder);

            if (createSerializer == null)
            {
                return false;
            }
            else
            {
                builder.Add(Expression.Call(
                    createSerializer,
                    GetInterfaceMethod(createSerializer.Type, typeof(ISerializer<>), nameof(ISerializer<object>.Write)),
                    builder.Formatter,
                    builder.TypedInstance));

                return true;
            }
        }

        private Expression WriteValueExpression(DelegateBuilder builder, Expression value)
        {
            // TypeConverter allows all classes to be converted to a string
            // (they have the ToString method), however, CanConvertFrom
            // returns false by default unless the class has a custom type
            // descriptor that actually checks for it. Use this to see if
            // we're serializing a straight forward type or if we need to
            // use a nested serializer for it.
            TypeConverter converter = TypeDescriptor.GetConverter(value.Type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return this.WriteSimpleTypeExpression(builder, value);
            }
            else
            {
                SerializeInstance serializer = this.CreateDelegate(value.Type, builder.MetadataBuilder);
                return Expression.Invoke(
                    Expression.Constant(serializer),
                    builder.Formatter,
                    builder.Metadata,
                    value);
            }
        }

        private void WriteValueType(Type type, DelegateBuilder builder)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (!type.IsEnum)
            {
                throw new InvalidOperationException("Unable to serialize value types.");
            }

            // We need to cast to the underlying type rather then the nullable
            // type, hence we can't use TypedInstance
            int index = builder.MetadataBuilder.GetOrAddMetadata(type);
            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.Formatter.WriteBeginPrimitive,
                Expression.MakeIndex(
                    builder.Metadata,
                    this.Methods.ReadOnlyList.Item,
                    new[] { Expression.Constant(index) })));

            builder.Add(this.CallWriteEnumExpression(
                builder,
                Expression.Convert(builder.RawInstance, type),
                type));

            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.Formatter.WriteEndPrimitive));
        }
    }
}
