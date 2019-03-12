namespace Host.UnitTests.Serialization
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
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

        protected class CustomSerializer : ISerializer<PrimitiveProperty>
        {
            internal static readonly object SyncRoot = new object();
            private static PrimitiveProperty currentValue;

            public PrimitiveProperty Read(IClassReader reader)
            {
                return GetLastWritten();
            }

            public void Write(IClassWriter writer, PrimitiveProperty instance)
            {
                currentValue = instance;
            }

            internal static PrimitiveProperty GetLastWritten()
            {
                PrimitiveProperty value = currentValue;
                currentValue = null;
                return value;
            }

            internal static void SetNextRead(PrimitiveProperty value)
            {
                currentValue = value;
            }
        }

        protected class CyclicReference
        {
            public CyclicReference Value { get; set; }
        }

        protected class EnumProperty
        {
            public FakeLongEnum Value { get; set; }
        }

        protected class NestedSerializer : ISerializer<WithNestedType>
        {
            private readonly ISerializer<PrimitiveProperty> nestedSerializer;

            public NestedSerializer(ISerializer<PrimitiveProperty> nestedSerializer)
            {
                this.nestedSerializer = nestedSerializer;
            }

            public WithNestedType Read(IClassReader reader)
            {
                return new WithNestedType
                {
                    Nested = this.nestedSerializer.Read(reader)
                };
            }

            public void Write(IClassWriter writer, WithNestedType instance)
            {
                this.nestedSerializer.Write(writer, instance.Nested);
            }
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

        protected class SecondSerializer : CustomSerializer
        {
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
