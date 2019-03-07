// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Host.Engine;
    using Crest.Host.Serialization.Internal;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Contains the generated delegates for the specified formatter.
    /// </summary>
    /// <typeparam name="T">The type of the formatter class.</typeparam>
    [SingleInstance]
    internal sealed class FormatterSerializer<T> : ISerializerGenerator<T>
        where T : IFormatter
    {
        private readonly Func<Stream, SerializationMode, T> createFormatter;
        private readonly DeserializeDelegateGenerator deserializeGenerator;
        private readonly SerializeDelegateGenerator serializeGenerator;
        private ImmutableArray<object> deserializeMetadata;
        private ImmutableArray<object> serializeMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatterSerializer{T}"/> class.
        /// </summary>
        /// <param name="discoveredTypes">The available types.</param>
        public FormatterSerializer(DiscoveredTypes discoveredTypes)
        {
            this.createFormatter = CallFormatterConstructor();

            var metadataBuilder = new MetadataBuilder(0);
            this.deserializeGenerator = new DeserializeDelegateGenerator(discoveredTypes, metadataBuilder);
            this.deserializeMetadata = ImmutableArray.CreateRange(metadataBuilder.CreateMetadata<T>());

            metadataBuilder = new MetadataBuilder(0);
            this.serializeGenerator = new SerializeDelegateGenerator(discoveredTypes, metadataBuilder);
            this.serializeMetadata = ImmutableArray.CreateRange(metadataBuilder.CreateMetadata<T>());
        }

        /// <inheritdoc />
        public object Deserialize(Stream stream, Type type)
        {
            DeserializeInstance deserialize = GetDelegate(
                this.deserializeGenerator,
                ref this.deserializeMetadata,
                type);

            T formatter = default;
            try
            {
                formatter = this.createFormatter(stream, SerializationMode.Deserialize);
                return deserialize(formatter, this.deserializeMetadata);
            }
            finally
            {
                if (formatter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public void Prime(Type classType)
        {
            if (classType.GetConstructor(Type.EmptyTypes) != null)
            {
                GetDelegate(this.deserializeGenerator, ref this.deserializeMetadata, classType);
            }

            GetDelegate(this.serializeGenerator, ref this.serializeMetadata, classType);
        }

        /// <inheritdoc />
        public void Serialize(Stream stream, object value)
        {
            SerializeInstance serialize = GetDelegate(
                this.serializeGenerator,
                ref this.serializeMetadata,
                value.GetType());

            T formatter = default;
            try
            {
                formatter = this.createFormatter(stream, SerializationMode.Serialize);
                serialize(formatter, this.serializeMetadata, value);
                formatter.Writer.Flush();
            }
            finally
            {
                if (formatter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static Func<Stream, SerializationMode, T> CallFormatterConstructor()
        {
            ConstructorInfo constructor = typeof(T).GetConstructor(
                new[] { typeof(Stream), typeof(SerializationMode) });
            Assert(constructor != null, "Type must have a public constructor accepting Stream+SerializationMode");

            ParameterExpression stream = Expression.Parameter(typeof(Stream));
            ParameterExpression mode = Expression.Parameter(typeof(SerializationMode));
            return Expression.Lambda<Func<Stream, SerializationMode, T>>(
                Expression.New(constructor, new[] { stream, mode }),
                new[] { stream, mode })
                .Compile();
        }

        private static TDelegate GetDelegate<TDelegate>(
            DelegateGenerator<TDelegate> generator,
            ref ImmutableArray<object> metadata,
            Type type)
            where TDelegate : Delegate
        {
            // As we're single instance, we need to make sure we're thread safe
            // in this part (hence the use of immutable array, so that once
            // we've returned the delegate others can still use it without the
            // need for more locks as the contents of metadata will not change)
            lock (generator)
            {
                if (!generator.TryGetDelegate(type, out TDelegate action))
                {
                    var metadataBuilder = new MetadataBuilder(metadata.Length);
                    action = generator.CreateDelegate(type, metadataBuilder);
                    metadata = metadata.AddRange(metadataBuilder.CreateMetadata<T>());
                }

                return action;
            }
        }
    }
}
