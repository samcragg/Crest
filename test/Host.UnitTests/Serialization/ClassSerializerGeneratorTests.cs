namespace Host.UnitTests.Serialization
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ClassSerializerGeneratorTests
    {
        private readonly ModuleBuilder module;
        private Type nestedSerializerType;

        protected ClassSerializerGeneratorTests()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnitTestDynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            this.module = assemblyBuilder.DefineDynamicModule("Module");
        }

        private Type GenerateSerializer(Type type)
        {
            return this.nestedSerializerType;
        }

        public sealed class Constructor : ClassSerializerGeneratorTests
        {
            [Fact]
            public void ShouldEnsureTheBaseClassHasTheStaticMethodGetMetadata()
            {
                Action action = () => new ClassSerializerGenerator(
                    this.GenerateSerializer,
                    this.module,
                    typeof(WithoutGetMetadata));

                action.Should().Throw<InvalidOperationException>()
                      .WithMessage("*GetMetadata*");
            }

            [Fact]
            public void ShouldEnsureTheBaseClassImplementIIClassSerializer()
            {
                Action action = () => new ClassSerializerGenerator(
                    this.GenerateSerializer,
                    this.module,
                    typeof(DoesNotImplementIClassSerializer));

                action.Should().Throw<InvalidOperationException>()
                      .WithMessage("*IClassSerializer*");
            }

            public class ClassSerializerMethods : IClassSerializer<string>
            {
                public ValueReader Reader => null;

                public ValueWriter Writer => null;

                public void BeginRead(string metadata)
                {
                }

                public void EndRead()
                {
                }

                public void ReadBeginClass(string metadata)
                {
                }

                public string ReadBeginProperty()
                {
                    return null;
                }

                public void ReadEndClass()
                {
                }

                public void ReadEndProperty()
                {
                }

                public void WriteBeginClass(string metadata)
                {
                }

                public void WriteBeginProperty(string propertyMetadata)
                {
                }

                public void WriteEndClass()
                {
                }

                public void WriteEndProperty()
                {
                }

                void IPrimitiveSerializer<string>.BeginWrite(string metadata)
                {
                }

                void IPrimitiveSerializer<string>.EndWrite()
                {
                }

                void IPrimitiveSerializer<string>.Flush()
                {
                }

                bool IArraySerializer.ReadBeginArray(Type elementType)
                {
                    return false;
                }

                bool IArraySerializer.ReadElementSeparator()
                {
                    return false;
                }

                void IArraySerializer.ReadEndArray()
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

            public class DoesNotImplementIClassSerializer
            {
            }

            public class WithoutGetMetadata : ClassSerializerMethods
            {
                protected WithoutGetMetadata(Stream stream)
                {
                }

                protected WithoutGetMetadata(ValueWriter writer)
                {
                }
            }
        }

        [Collection(nameof(FakeSerializerBase.OutputEnumNames))]
        public sealed class GenerateFor : ClassSerializerGeneratorTests
        {
            private readonly ClassSerializerGenerator generator;

            public GenerateFor()
            {
                this.generator = new ClassSerializerGenerator(
                    this.GenerateSerializer,
                    this.module,
                    typeof(FakeSerializerBase));
            }

            public enum FakeLongEnum : long
            {
                Value = 1
            }

            [Fact]
            public void ShouldCallReadBeginClass()
            {
                FakeSerializerBase serializer = this.CreateSerializer<PrimitiveProperty>();

                ((ITypeSerializer)serializer).Read();

                serializer.ReadBeginClassCount.Should().Be(1);
            }

            [Fact]
            public void ShouldCallReadEndClass()
            {
                FakeSerializerBase serializer = this.CreateSerializer<PrimitiveProperty>();

                ((ITypeSerializer)serializer).Read();

                serializer.ReadEndClassCount.Should().Be(1);
            }

            [Fact]
            public void ShouldCallReadEndProperty()
            {
                FakeSerializerBase serializer = this.CreateSerializer<PrimitiveProperty>();
                serializer.Properties.Add(nameof(PrimitiveProperty.Value));

                ((ITypeSerializer)serializer).Read();

                serializer.ReadEndPropertyCount.Should().Be(1);
            }

            [Fact]
            public void ShouldCallTheParentSerializerConstructor()
            {
                Type type = this.generator.GenerateFor(typeof(PrimitiveProperty));

                FakeSerializerBase parent = Substitute.For<FakeSerializerBase>(Stream.Null, SerializationMode.Serialize);
                var instance = (FakeSerializerBase)Activator.CreateInstance(type, parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }

            [Fact]
            public void ShouldCallTheStreamConstructor()
            {
                Stream stream = Substitute.For<Stream>();
                Type type = this.generator.GenerateFor(typeof(PrimitiveProperty));

                var instance = (FakeSerializerBase)Activator.CreateInstance(type, stream, SerializationMode.Serialize);

                instance.Stream.Should().BeSameAs(stream);
            }

            [Fact]
            public void ShouldEnsureTheTypeHasAPublicDefaultConstructor()
            {
                Action action = () => this.generator.GenerateFor(typeof(NoDefaultConstructor));

                action.Should().Throw<InvalidOperationException>()
                      .WithMessage("*" + nameof(NoDefaultConstructor) + "*");
            }

            [Fact]
            public void ShouldHandleBrowsableAttributes()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new BrowsableProperties
                    {
                        BrowsableFalse = 1,
                        BrowsableTrue = 2
                    });

                serializer.Writer.DidNotReceive().WriteInt32(1);
                serializer.Writer.Received().WriteInt32(2);
            }

            [Fact]
            public void ShouldIncludeReadArrayMethod()
            {
                FakeSerializerBase serializer = this.CreateSerializer<PrimitiveProperty>();
                serializer.SetArray(r => r.ReadInt32(), 1);
                serializer.Properties.Add(nameof(PrimitiveProperty.Value));

                Array result = ((ITypeSerializer)serializer).ReadArray();

                result.Should().BeOfType<PrimitiveProperty[]>()
                      .Which.Single().Value.Should().Be(1);
            }

            [Fact]
            public void ShouldIncludeWriteArrayMethod()
            {
                Type type = this.generator.GenerateFor(typeof(PrimitiveProperty));
                var serializer = (FakeSerializerBase)Activator.CreateInstance(type, Stream.Null, SerializationMode.Serialize);

                ((ITypeSerializer)serializer).WriteArray(new[] { new PrimitiveProperty { Value = 1 } });

                serializer.Writer.Received().WriteInt32(1);
            }

            [Fact]
            public void ShouldNotSerializeNullablePropertiesWithoutValues()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new NullableProperty { Value = null });

                serializer.Writer.ReceivedCalls().Should().BeEmpty();
            }

            [Fact]
            public void ShouldNotSerializeReferencePropertiesWithoutValues()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new ReferenceProperty { Value = null });

                serializer.Writer.ReceivedCalls().Should().BeEmpty();
            }

            [Fact]
            public void ShouldReadArrayOfEnumElements()
            {
                FakeSerializerBase.OutputEnumNames = false;
                FakeSerializerBase serializer = this.CreateSerializer<ArrayProperty>();
                serializer.SetArray(r => r.ReadInt64(), 1);
                serializer.Properties.Add(nameof(ArrayProperty.EnumValues));

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<ArrayProperty>()
                      .Which.EnumValues.Should().Equal(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadArrayOfPrimitivesElements()
            {
                FakeSerializerBase serializer = this.CreateSerializer<ArrayProperty>();
                serializer.SetArray(r => r.ReadInt32(), 1);
                serializer.Properties.Add(nameof(ArrayProperty.IntValues));

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<ArrayProperty>()
                      .Which.IntValues.Should().Equal(1);
            }

            [Fact]
            public void ShouldReadEnumsAsNames()
            {
                FakeSerializerBase.OutputEnumNames = true;
                FakeSerializerBase serializer = this.CreateSerializer<EnumProperty>();
                serializer.Properties.Add(nameof(EnumProperty.Value));
                serializer.Reader.ReadString().Returns(nameof(FakeLongEnum.Value));

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<EnumProperty>()
                      .Which.Value.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadEnumsAsNumbers()
            {
                FakeSerializerBase.OutputEnumNames = false;
                FakeSerializerBase serializer = this.CreateSerializer<EnumProperty>();
                serializer.Properties.Add(nameof(EnumProperty.Value));
                serializer.Reader.ReadInt64().Returns((long)FakeLongEnum.Value);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<EnumProperty>()
                      .Which.Value.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadFromMultipleProperties()
            {
                FakeSerializerBase serializer = this.CreateSerializer<MultipleProperties>();
                serializer.Properties.Add(nameof(MultipleProperties.Value9));
                serializer.Reader.ReadInt32().Returns(1);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<MultipleProperties>()
                      .Which.Value9.Should().Be(1);
            }

            [Fact]
            public void ShouldReadNestedTypes()
            {
                this.nestedSerializerType = this.generator.GenerateFor(typeof(PrimitiveProperty));

                FakeSerializerBase serializer = this.CreateSerializer<WithNestedType>();
                serializer.Properties.Add(nameof(WithNestedType.Nested));
                serializer.Properties.Add(nameof(PrimitiveProperty.Value));
                serializer.Reader.ReadInt32().Returns(123);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<WithNestedType>()
                      .Which.Nested.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReadNullablePropertiesThatAreNull()
            {
                FakeSerializerBase serializer = this.CreateSerializer<NullableProperty>();
                serializer.Properties.Add(nameof(NullableProperty.Value));
                serializer.Reader.ReadNull().Returns(true);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<NullableProperty>()
                      .Which.Value.Should().BeNull();

                // Also make sure it didn't try to read anything from the stream
                serializer.Reader.DidNotReceive().ReadInt32();
            }

            [Fact]
            public void ShouldReadNullablePropertiesWithValues()
            {
                FakeSerializerBase serializer = this.CreateSerializer<NullableProperty>();
                serializer.Properties.Add(nameof(NullableProperty.Value));
                serializer.Reader.ReadInt32().Returns(1);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<NullableProperty>()
                      .Which.Value.Should().Be(1);
            }

            [Fact]
            public void ShouldReadPrimitiveProperties()
            {
                FakeSerializerBase serializer = this.CreateSerializer<PrimitiveProperty>();
                serializer.Properties.Add(nameof(PrimitiveProperty.Value));
                serializer.Reader.ReadInt32().Returns(123);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<PrimitiveProperty>()
                      .Which.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReadPropertiesWithDisplayName()
            {
                FakeSerializerBase serializer = this.CreateSerializer<DisplayNameProperty>();
                serializer.Properties.Add("display");
                serializer.Reader.ReadInt32().Returns(1);

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<DisplayNameProperty>()
                      .Which.Value.Should().Be(1);
            }

            [Fact]
            public void ShouldReadReferencePropertiesWithValues()
            {
                FakeSerializerBase serializer = this.CreateSerializer<ReferenceProperty>();
                serializer.Properties.Add(nameof(ReferenceProperty.Value));
                serializer.Reader.ReadString().Returns("string");

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<ReferenceProperty>()
                      .Which.Value.Should().Be("string");
            }

            [Fact]
            public void ShouldReadUnknownTypesAsObject()
            {
                FakeSerializerBase serializer = this.CreateSerializer<ValueProperty>();
                serializer.Properties.Add(nameof(ValueProperty.Value));
                serializer.Reader.ReadObject(typeof(ExampleValueType)).Returns(new ExampleValueType(123));

                object result = ((ITypeSerializer)serializer).Read();

                result.Should().BeOfType<ValueProperty>()
                      .Which.Value.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldSerializeArrayOfEnumsElements()
            {
                FakeSerializerBase.OutputEnumNames = false;

                FakeSerializerBase serializer = this.SerializeValue(
                    new ArrayProperty { EnumValues = new FakeLongEnum?[] { FakeLongEnum.Value } });

                serializer.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldSerializeArrayOfPrimitivesElements()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new ArrayProperty { IntValues = new int?[] { 123 } });

                serializer.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldSerializeEnumValuesAsNames()
            {
                FakeSerializerBase.OutputEnumNames = true;
                FakeSerializerBase serializer = this.SerializeValue(
                    new EnumProperty { Value = FakeLongEnum.Value });

                serializer.Writer.Received().WriteString(nameof(FakeLongEnum.Value));
            }

            [Fact]
            public void ShouldSerializeEnumValuesAsNumbers()
            {
                FakeSerializerBase.OutputEnumNames = false;
                FakeSerializerBase serializer = this.SerializeValue(
                    new EnumProperty { Value = FakeLongEnum.Value });

                // Notice the call to the 64 bit version, as the base class of
                // the FakeLongEnum is long not int.
                serializer.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldSerializeNestedTypes()
            {
                var value = new WithNestedType
                {
                    Nested = new PrimitiveProperty { Value = 123 }
                };
                Type primitiveSerializer = this.generator.GenerateFor(typeof(PrimitiveProperty));
                this.nestedSerializerType = primitiveSerializer;

                FakeSerializerBase serializer = this.SerializeValue(value);

                serializer.BeginProperty.Should().Be(nameof(WithNestedType.Nested));

                // The same writer should have been passed to the nested serializer
                serializer.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldSerializeNullablePropertiesWithValues()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new NullableProperty { Value = 123 });

                serializer.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldSerializePrimitiveProperties()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new PrimitiveProperty { Value = 123 });

                serializer.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldSerializeReferencePropertiesWithValues()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new ReferenceProperty { Value = "123" });

                serializer.Writer.Received().WriteString("123");
            }

            [Fact]
            public void ShouldSerializeUnknownTypesAsObject()
            {
                FakeSerializerBase serializer = this.SerializeValue(
                    new ValueProperty());

                serializer.Writer.Received().WriteObject(Arg.Is<object>(o => o is ExampleValueType));
            }

            private FakeSerializerBase CreateSerializer<T>()
            {
                return (FakeSerializerBase)Activator.CreateInstance(
                    this.generator.GenerateFor(typeof(T)),
                    Stream.Null,
                    SerializationMode.Serialize);
            }

            private FakeSerializerBase SerializeValue<T>(T value)
            {
                FakeSerializerBase serializer = CreateSerializer<T>();
                ((ITypeSerializer)serializer).Write(value);
                return serializer;
            }

            // These have to be public so the generated code can access them
            [TypeConverter(typeof(Int16Converter))] // Value types need to have a value converter
            public struct ExampleValueType
            {
                public ExampleValueType(short value)
                {
                    this.Value = value;
                }

                public short Value { get; }
            }

            public class ArrayProperty
            {
                public FakeLongEnum?[] EnumValues { get; set; }

                public int?[] IntValues { get; set; }
            }

            public class BrowsableProperties
            {
                [Browsable(false)]
                public int BrowsableFalse { get; set; }

                [Browsable(true)]
                public int BrowsableTrue { get; set; }
            }

            public class DisplayNameProperty
            {
                [DisplayName("display")]
                public int Value { get; set; }
            }

            public class EnumProperty
            {
                public FakeLongEnum Value { get; set; }
            }

            public class MultipleProperties
            {
                public int Value1 { get; set; }
                public int Value2 { get; set; }
                public int Value3 { get; set; }
                public int Value4 { get; set; }
                public int Value5 { get; set; }
                public int Value6 { get; set; }
                public int Value7 { get; set; }
                public int Value8 { get; set; }
                public int Value9 { get; set; }
            }

            public class NoDefaultConstructor
            {
                public NoDefaultConstructor(int value)
                {
                }
            }

            public class NullableArrayProperty
            {
                public int?[] Values { get; set; }
            }

            public class NullableProperty
            {
                // Initialize it to a non-null value to prove we're overwriting the property
                public int? Value { get; set; } = int.MaxValue;
            }

            public class PrimitiveProperty
            {
                public int Value { get; set; }
            }

            public class ReferenceProperty
            {
                public string Value { get; set; }
            }

            public class StringArrayProperty
            {
                public string[] Values { get; set; }
            }

            public class ValueProperty
            {
                public ExampleValueType Value { get; set; }
            }

            public class WithNestedType
            {
                public PrimitiveProperty Nested { get; set; }
            }
        }
    }
}
