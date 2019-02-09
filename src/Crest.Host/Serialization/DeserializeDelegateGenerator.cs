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
    /// Deserializes a class from the formatter.
    /// </summary>
    /// <param name="formatter">Used to read the data stream.</param>
    /// <param name="metadata">Contains the pre-generated metadata.</param>
    /// <returns>The deserialized value.</returns>
    internal delegate object DeserializeInstance(IFormatter formatter, IReadOnlyList<object> metadata);

    /// <summary>
    /// Generates methods for deserializing a class.
    /// </summary>
    internal sealed partial class DeserializeDelegateGenerator : DelegateGenerator<DeserializeInstance>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializeDelegateGenerator"/> class.
        /// </summary>
        /// <param name="discoveredTypes">The available types.</param>
        /// <param name="metadataBuilder">Used to build the metadata for the type.</param>
        public DeserializeDelegateGenerator(
            DiscoveredTypes discoveredTypes,
            MetadataBuilder metadataBuilder)
            : base(discoveredTypes)
        {
            AddPrimitiveDelegates(this.KnownDelegates, metadataBuilder.GetOrAddMetadata);
        }

        /// <inheritdoc />
        protected override DeserializeInstance BuildDelegate(Type type, MetadataBuilder metadata)
        {
            var builder = new DelegateBuilder(type, metadata);

            if (type.IsArray)
            {
                this.ReadArray(type, builder);
            }
            else if (type.IsValueType)
            {
                this.ReadValueType(type, builder);
            }
            else if (!this.ReadFromCustomSerializer(type, builder))
            {
                this.ReadClass(type, builder);
            }

            return Expression.Lambda<DeserializeInstance>(
                    builder.BuildBody(this.Methods),
                    new[] { builder.Formatter, builder.Metadata })
                    .Compile();
        }

        private static string GetPropertyName(PropertyInfo property)
        {
            DisplayNameAttribute displayName =
                property.GetCustomAttribute<DisplayNameAttribute>();

            string name = displayName?.DisplayName ?? property.Name;
            return name.ToUpperInvariant();
        }

        private Expression CallReadEnumExpression(DelegateBuilder builder, Type enumType, Type nullableType)
        {
            Type underlyingType = enumType.GetEnumUnderlyingType();
            return Expression.Condition(
                Expression.Property(builder.Formatter, this.Methods.Formatter.EnumsAsIntegers),
                Expression.Convert(
                    this.CallReaderMethodExpression(builder, this.Methods.ValueReader[underlyingType]),
                    nullableType),
                Expression.Convert(
                    Expression.Call(
                        this.Methods.Enum.Parse,
                        Expression.Constant(enumType),
                        this.CallReaderMethodExpression(builder, this.Methods.ValueReader[typeof(string)]),
                        Expression.Constant(true)),
                    nullableType));
        }

        private Expression CallReaderMethodExpression(DelegateBuilder builder, MethodInfo method)
        {
            return Expression.Call(
                Expression.Property(builder.Formatter, this.Methods.ClassReader.GetReader),
                method);
        }

        private void ReadArray(Type type, DelegateBuilder builder)
        {
            Type elementType = type.GetElementType();
            DeserializeInstance readElement = this.CreateDelegate(elementType, builder.MetadataBuilder);

            MethodInfo readArrayMethod = typeof(Adapter)
                .GetMethod(nameof(Adapter.ReadArray))
                .MakeGenericMethod(elementType);

            builder.InitializeInstance = Expression.Call(
                readArrayMethod,
                builder.Formatter,
                builder.Metadata,
                Expression.Constant(readElement));
        }

        private void ReadClass(Type type, DelegateBuilder builder)
        {
            int index = builder.MetadataBuilder.GetOrAddMetadata(type);
            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.Formatter.ReadBeginClass,
                Expression.MakeIndex(
                    builder.Metadata,
                    this.Methods.ReadOnlyList.Item,
                    new[] { Expression.Constant(index) })));

            this.ReadProperties(type, builder);

            builder.Add(Expression.Call(
                builder.Formatter,
                this.Methods.ClassReader.ReadEndClass));
        }

        private bool ReadFromCustomSerializer(Type type, DelegateBuilder builder)
        {
            Type serializer = this.GetCustomSerializer(type);
            if (serializer == null)
            {
                return false;
            }
            else
            {
                builder.InitializeInstance = Expression.Call(
                    Expression.New(serializer),
                    GetInterfaceMethod(serializer, typeof(ICustomSerializer<>), nameof(ICustomSerializer<object>.Read)),
                    builder.Formatter);

                return true;
            }
        }

        private Expression ReadNullableValueExpression(DelegateBuilder builder, Expression value)
        {
            return Expression.Condition(
                this.CallReaderMethodExpression(builder, this.Methods.ValueReader.ReadNull),
                Expression.Constant(null, value.Type),
                value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "S3220:Method calls should not resolve ambiguously to overloads with \"params\"",
            Justification = "The overload is an optimization provided by the framework")]
        private void ReadProperties(Type type, DelegateBuilder builder)
        {
            var jumpTable = new JumpTableGenerator(this.Methods);
            foreach (PropertyInfo property in GetProperties(type))
            {
                jumpTable.Add(
                    GetPropertyName(property),
                    this.ReadPropertyExpression(builder, property));
            }

            LabelTarget breakLabel = Expression.Label("endWhile");
            ParameterExpression propertyName = Expression.Variable(typeof(string), "property");

            builder.Add(
                Expression.Loop(
                    Expression.Block(
                        new[] { propertyName },
                        Expression.Assign(
                            propertyName,
                            Expression.Call(
                                builder.Formatter,
                                this.Methods.ClassReader.ReadBeginProperty)),
                        Expression.IfThenElse(
                            Expression.Equal(propertyName, Expression.Constant(null)),
                            Expression.Break(breakLabel),
                            jumpTable.Build(propertyName))),
                    breakLabel));
        }

        private Expression ReadPropertyExpression(DelegateBuilder builder, PropertyInfo property)
        {
            return Expression.Block(
                Expression.Assign(
                    Expression.Property(builder.Instance, property),
                    this.ReadValueExpression(builder, property.PropertyType)),
                Expression.Call(
                    builder.Formatter,
                    this.Methods.ClassReader.ReadEndProperty));
        }

        private Expression ReadSimpleTypeExpression(DelegateBuilder builder, Type type)
        {
            Type rawType = Nullable.GetUnderlyingType(type) ?? type;
            Expression readValue = this.ReadValueTypeExpression(builder, rawType);

            if (rawType == type)
            {
                return readValue;
            }
            else
            {
                return this.ReadNullableValueExpression(
                    builder,
                    Expression.Convert(readValue, type));
            }
        }

        private Expression ReadValueExpression(DelegateBuilder builder, Type type)
        {
            // See comments in SerializeDelegateGenerator.WriteValueExpression
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return this.ReadSimpleTypeExpression(builder, type);
            }
            else
            {
                DeserializeInstance deserializer = this.CreateDelegate(type, builder.MetadataBuilder);
                return Expression.Convert(
                    Expression.Invoke(
                        Expression.Constant(deserializer),
                        builder.Formatter,
                        builder.Metadata),
                    type);
            }
        }

        private void ReadValueType(Type type, DelegateBuilder builder)
        {
            Type rawType = Nullable.GetUnderlyingType(type) ?? type;
            if (!rawType.IsEnum)
            {
                throw new InvalidOperationException("Unable to serialize value types.");
            }

            int index = builder.MetadataBuilder.GetOrAddMetadata(type);
            Expression readEnum = Expression.Block(
                Expression.Call(
                    builder.Formatter,
                    this.Methods.Formatter.ReadBeginPrimitive,
                    Expression.MakeIndex(
                        builder.Metadata,
                        this.Methods.ReadOnlyList.Item,
                        new[] { Expression.Constant(index) })),
                Expression.Assign(builder.Instance, this.CallReadEnumExpression(builder, rawType, type)),
                Expression.Call(builder.Formatter, this.Methods.Formatter.ReadEndPrimitive));

            builder.InitializeInstance = Expression.Default(type);
            if (type != rawType)
            {
                builder.Add(Expression.IfThen(
                    Expression.IsFalse(this.CallReaderMethodExpression(builder, this.Methods.ValueReader.ReadNull)),
                    readEnum));
            }
            else
            {
                builder.Add(readEnum);
            }
        }

        private Expression ReadValueTypeExpression(DelegateBuilder builder, Type type)
        {
            if (type.IsEnum)
            {
                return this.CallReadEnumExpression(builder, type, type);
            }
            else
            {
                Expression readValue;
                if (this.Methods.ValueReader.TryGetMethod(type, out MethodInfo readMethod))
                {
                    readValue = this.CallReaderMethodExpression(builder, readMethod);
                }
                else
                {
                    readValue = Expression.Convert(
                        Expression.Call(
                            Expression.Property(builder.Formatter, this.Methods.ClassReader.GetReader),
                            this.Methods.ValueReader.ReadObject,
                            Expression.Constant(type)),
                        type);
                }

                return type.IsValueType ?
                    readValue :
                    this.ReadNullableValueExpression(builder, readValue);
            }
        }
    }
}
