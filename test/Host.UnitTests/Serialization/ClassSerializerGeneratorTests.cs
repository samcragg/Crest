namespace Host.UnitTests.Serialization
{
    using System;
    using System.ComponentModel;
    using System.IO;
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
                    new EnumProperty { Enum = FakeLongEnum.Value });

                serializer.Writer.Received().WriteString(nameof(FakeLongEnum.Value));
            }

            [Fact]
            public void ShouldSerializeEnumValuesAsNumbers()
            {
                FakeSerializerBase.OutputEnumNames = false;
                FakeSerializerBase serializer = this.SerializeValue(
                    new EnumProperty { Enum = FakeLongEnum.Value });

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

            private FakeSerializerBase SerializeValue<T>(T value)
            {
                Type type = this.generator.GenerateFor(typeof(T));
                object instance = Activator.CreateInstance(type, Stream.Null, SerializationMode.Serialize);

                ((ITypeSerializer)instance).Write(value);
                return (FakeSerializerBase)instance;
            }

            // These have to be public so the generated code can access them
            [TypeConverter(typeof(Int16Converter))] // Value types need to have a value converter
            public struct ExampleValueType
            {
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

            public class EnumProperty
            {
                public FakeLongEnum Enum { get; set; }
            }

            public class NullableArrayProperty
            {
                public int?[] Values { get; set; }
            }

            public class NullableProperty
            {
                public int? Value { get; set; }
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
