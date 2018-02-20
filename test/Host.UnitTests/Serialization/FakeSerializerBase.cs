namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using Crest.Host.Serialization.Internal;
    using NSubstitute;

    public class FakeSerializerBase : IClassSerializer<string>, IArraySerializer
    {
        protected FakeSerializerBase(Stream stream, SerializationMode mode)
        {
            this.Mode = mode;
            this.Stream = stream;
            this.Writer = Substitute.For<ValueWriter>();
        }

        protected FakeSerializerBase(FakeSerializerBase parent)
        {
            this.Writer = parent.Writer;
        }

        public static bool OutputEnumNames { get; set; }

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

        public void BeginWrite(string metadata)
        {
            this.BeginPrimitive = metadata;
        }

        public void EndWrite()
        {
        }

        public void Flush()
        {
        }

        public void WriteBeginClass(string metadata)
        {
            this.BeginClass = metadata;
        }

        public void WriteBeginProperty(string propertyMetadata)
        {
            this.BeginProperty = propertyMetadata;
        }

        public void WriteEndClass()
        {
        }

        public void WriteEndProperty()
        {
        }

        void IArraySerializer.WriteBeginArray(Type elementType, int size)
        {
        }

        void IArraySerializer.WriteElementSeparator()
        {
        }

        void IArraySerializer.WriteEndArray()
        {
        }
    }
}
