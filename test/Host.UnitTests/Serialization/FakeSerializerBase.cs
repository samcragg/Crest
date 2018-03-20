namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Serialization.Internal;
    using NSubstitute;

    public class FakeSerializerBase : IClassSerializer<string>
    {
        private int arrayCount;
        private int arrayIndex;

        protected FakeSerializerBase(Stream stream, SerializationMode mode)
            : this()
        {
            this.Mode = mode;
            this.Stream = stream;
        }

        protected FakeSerializerBase(FakeSerializerBase parent)
        {
            this.Reader = parent.Reader;
            this.Writer = parent.Writer;
        }

        protected FakeSerializerBase()
        {
            this.Reader = Substitute.For<ValueReader>();
            this.Writer = Substitute.For<ValueWriter>();
        }

        public static bool OutputEnumNames { get; set; }

        public ValueReader Reader { get; }

        public ValueWriter Writer { get; }

        internal string BeginClass { get; private set; }

        internal string BeginPrimitive { get; private set; }

        internal string BeginProperty { get; private set; }

        internal SerializationMode Mode { get; }

        internal Stream Stream { get; }

        public static string GetMetadata(PropertyInfo property)
        {
            return property.Name;
        }

        public static string GetTypeMetadata(Type type)
        {
            return type.Name;
        }

        public void BeginRead(string metadata)
        {
        }

        public virtual void BeginWrite(string metadata)
        {
            this.BeginPrimitive = metadata;
        }

        public void EndRead()
        {
        }

        public virtual void EndWrite()
        {
        }

        public virtual void Flush()
        {
        }

        public virtual bool ReadBeginArray(Type elementType)
        {
            return this.arrayCount > 0;
        }

        public virtual bool ReadElementSeparator()
        {
            this.arrayIndex++;
            return this.arrayIndex < this.arrayCount;
        }

        public virtual void ReadEndArray()
        {
        }

        public virtual void WriteBeginArray(Type elementType, int size)
        {
        }

        public virtual void WriteBeginClass(string metadata)
        {
            this.BeginClass = metadata;
        }

        public virtual void WriteBeginProperty(string propertyMetadata)
        {
            this.BeginProperty = propertyMetadata;
        }

        public virtual void WriteElementSeparator()
        {
        }

        public virtual void WriteEndArray()
        {
        }

        public virtual void WriteEndClass()
        {
        }

        public virtual void WriteEndProperty()
        {
        }

        internal void SetArray<T>(Func<ValueReader, T> read, params T[] values)
        {
            this.arrayCount = values.Length;
            if (values.Length > 0)
            {
                IEnumerable<bool> isNull = values.Select(v => v == null);
                this.Reader.ReadNull().Returns(isNull.First(), isNull.Skip(1).ToArray());

                read(this.Reader).Returns(values[0], values.Skip(1).ToArray());
            }
        }
    }
}
