// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Allows serialization of types with custom logic.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <typeparam name="TFormatter">The serialization format type.</typeparam>
    /// <typeparam name="TCustom">The custom serializer type.</typeparam>
    public sealed class CustomSerializerAdapter<T, TFormatter, TCustom> : ITypeSerializer
        where TFormatter : IClassReader, IClassWriter
        where TCustom : ICustomSerializer<T>, new()
    {
        // Avoid calling Activator.CreateInstance - this is about 5x faster (see
        // https://stackoverflow.com/questions/367577)
        private static readonly Func<TCustom> CreateCustomSerializer =
            ((Expression<Func<TCustom>>)(() => new TCustom())).Compile();

        private static readonly Func<Stream, SerializationMode, TFormatter> CreateFormatter =
            CallFormatterConstructor();

        private readonly IClassReader reader;
        private readonly TCustom serializer;
        private readonly IClassWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSerializerAdapter{T, TClassSerializer, TCustomSerializer}"/> class.
        /// </summary>
        /// <param name="formatter">Used to format the serialized data.</param>
        public CustomSerializerAdapter(TFormatter formatter)
        {
            this.reader = formatter;
            this.writer = formatter;
            this.serializer = CreateCustomSerializer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSerializerAdapter{T, TClassSerializer, TCustomSerializer}"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from/write to.</param>
        /// <param name="mode">The serialization mode.</param>
        public CustomSerializerAdapter(Stream stream, SerializationMode mode)
            : this(CreateFormatter(stream, mode))
        {
        }

        /// <inheritdoc />
        public void Flush()
        {
            this.writer.Writer.Flush();
        }

        /// <inheritdoc />
        public object Read()
        {
            return this.serializer.Read(this.reader);
        }

        /// <inheritdoc />
        public Array ReadArray()
        {
            ArrayBuffer<T> buffer = default;
            if (this.reader.ReadBeginArray(typeof(T)))
            {
                do
                {
                    if (this.reader.Reader.ReadNull())
                    {
                        buffer.Add(default);
                    }
                    else
                    {
                        buffer.Add(this.serializer.Read(this.reader));
                    }
                }
                while (this.reader.ReadElementSeparator());

                this.reader.ReadEndArray();
            }

            return buffer.ToArray();
        }

        /// <inheritdoc />
        public void Write(object instance)
        {
            this.serializer.Write(this.writer, (T)instance);
        }

        /// <inheritdoc />
        public void WriteArray(Array array)
        {
            var typedArray = (T[])array;
            this.writer.WriteBeginArray(typeof(T), typedArray.Length);
            if (typedArray.Length > 0)
            {
                this.WriteNullableValue(typedArray[0]);
                for (int i = 1; i < typedArray.Length; i++)
                {
                    this.writer.WriteElementSeparator();
                    this.WriteNullableValue(typedArray[i]);
                }
            }

            this.writer.WriteEndArray();
        }

        private static Func<Stream, SerializationMode, TFormatter> CallFormatterConstructor()
        {
            ConstructorInfo constructor = typeof(TFormatter).GetConstructor(
                new[] { typeof(Stream), typeof(SerializationMode) });

            ParameterExpression stream = Expression.Parameter(typeof(Stream));
            ParameterExpression mode = Expression.Parameter(typeof(SerializationMode));
            return Expression.Lambda<Func<Stream, SerializationMode, TFormatter>>(
                Expression.New(constructor, stream, mode),
                new[] { stream, mode })
                .Compile();
        }

        private void WriteNullableValue(T value)
        {
            if (value == null)
            {
                this.writer.Writer.WriteNull();
            }
            else
            {
                this.serializer.Write(this.writer, value);
            }
        }
    }
}
