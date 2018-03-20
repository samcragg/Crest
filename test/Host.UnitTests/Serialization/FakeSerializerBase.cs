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
        private readonly FakeSerializerBase parent;
        private int arrayCount;
        private int arrayIndex;
        private int propertyIndex = -1;

        protected FakeSerializerBase(Stream stream, SerializationMode mode)
            : this()
        {
            this.Mode = mode;
            this.Stream = stream;
        }

        protected FakeSerializerBase(FakeSerializerBase parent)
        {
            this.parent = parent;
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

        internal List<string> Properties { get; } = new List<string>();

        internal int ReadBeginClassCount { get; private set; }

        internal int ReadEndClassCount { get; private set; }

        internal int ReadEndPropertyCount { get; private set; }

        internal Stream Stream { get; }

        public static string GetMetadata(PropertyInfo property)
        {
            return property.Name;
        }

        public static string GetTypeMetadata(Type type)
        {
            return type.Name;
        }

        public virtual void BeginRead(string metadata)
        {
        }

        public virtual void BeginWrite(string metadata)
        {
            this.BeginPrimitive = metadata;
        }

        public virtual void EndRead()
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

        public virtual void ReadBeginClass(string metadata)
        {
            this.ReadBeginClassCount++;
        }

        public virtual string ReadBeginProperty()
        {
            if (this.parent != null)
            {
                return this.parent.ReadBeginProperty();
            }
            else
            {
                if (++this.propertyIndex >= this.Properties.Count)
                {
                    return default;
                }

                return this.Properties[this.propertyIndex];
            }
        }

        public virtual bool ReadElementSeparator()
        {
            this.arrayIndex++;
            return this.arrayIndex < this.arrayCount;
        }

        public virtual void ReadEndArray()
        {
        }

        public virtual void ReadEndClass()
        {
            this.ReadEndClassCount++;
        }

        public virtual void ReadEndProperty()
        {
            this.ReadEndPropertyCount++;
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
