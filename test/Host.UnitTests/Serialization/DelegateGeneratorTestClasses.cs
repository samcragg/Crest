namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;

    public abstract class DelegateGeneratorClasses
    {
        protected enum FakeLongEnum : long
        {
            Value = 1
        }

        // Value types need to have a value converter
        [TypeConverter(typeof(Int16Converter))]
        protected struct ExampleValueType
        {
            public ExampleValueType(short value)
            {
                this.Value = value;
            }

            public short Value { get; }
        }

        protected struct InvalidStruct
        {
        }

        internal class FakeMetadataBuilder : MetadataBuilder
        {
            private readonly List<MemberInfo> metadata = new List<MemberInfo>();

            public FakeMetadataBuilder() : base(0)
            {
            }

            public override IReadOnlyList<object> CreateMetadata<T>()
            {
                return this.metadata.ToArray();
            }

            public override int GetOrAddMetadata(MemberInfo member)
            {
                this.metadata.Add(member);
                return this.metadata.Count - 1;
            }

            internal object GetMetadata(string name)
            {
                return this.metadata.FirstOrDefault(x => x.Name == name);
            }
        }

        protected class ArrayProperty
        {
            public FakeLongEnum?[] EnumValues { get; set; }

            public int?[] IntValues { get; set; }
        }

        protected class BrowsableProperties
        {
            [Browsable(false)]
            public int BrowsableFalse { get; set; }

            [Browsable(true)]
            public int BrowsableTrue { get; set; }
        }

        protected class CustomNestedSerializer : ISerializer<WithNestedType>
        {
            internal static readonly object SyncRoot = new object();
            private static WithNestedType currentValue;

            public CustomNestedSerializer(ISerializer<PrimitiveProperty> nestedSerializer)
            {
                this.NestedSerializer = nestedSerializer;
            }

            internal ISerializer<PrimitiveProperty> NestedSerializer { get; }

            public WithNestedType Read(IClassReader reader)
            {
                this.NestedSerializer.Read(reader);
                return GetLastWritten();
            }

            public void Write(IClassWriter writer, WithNestedType instance)
            {
                this.NestedSerializer.Write(writer, instance.Nested);
                currentValue = instance;
            }

            internal static WithNestedType GetLastWritten()
            {
                WithNestedType value = currentValue;
                currentValue = null;
                return value;
            }

            internal static void SetNextRead(WithNestedType value)
            {
                currentValue = value;
            }
        }

        protected class CustomPrimitiveSerializer : ISerializer<PrimitiveProperty>
        {
            public PrimitiveProperty Read(IClassReader reader)
            {
                throw new NotImplementedException();
            }

            public void Write(IClassWriter writer, PrimitiveProperty instance)
            {
                throw new NotImplementedException();
            }
        }

        protected class CyclicReference
        {
            public CyclicReference Value { get; set; }
        }

        protected class DataMemberProperties
        {
            public string B { get; set; }

            public string A { get; set; }

            [DataMember(Order = 2)]
            public string C2 { get; set; }

            [DataMember(Order = 1)]
            public string D1 { get; set; }
        }

        protected class EnumProperty
        {
            public FakeLongEnum Value { get; set; }
        }

        protected class NullableProperty
        {
            // Initialize it to a non-null value to prove we're overwriting the property
            public int? Value { get; set; } = int.MaxValue;
        }

        protected class PrimitiveProperty
        {
            public int Value { get; set; }
        }

        protected class ReferenceProperty
        {
            public string Value { get; set; }
        }

        protected class SecondSerializer : CustomPrimitiveSerializer
        {
        }

        protected class SerializerWithInvalidArguments : CustomPrimitiveSerializer
        {
            public SerializerWithInvalidArguments(IEnumerable<string> values)
            {
            }
        }

        protected class SerializerWithMultipleConstructors : CustomPrimitiveSerializer
        {
            public SerializerWithMultipleConstructors()
            {
            }

            public SerializerWithMultipleConstructors(ISerializer<DateTime> serializer)
            {
            }
        }

        protected class ValueProperty
        {
            public ExampleValueType Value { get; set; }
        }

        protected class WithNestedType
        {
            public PrimitiveProperty Nested { get; set; }
        }
    }
}
