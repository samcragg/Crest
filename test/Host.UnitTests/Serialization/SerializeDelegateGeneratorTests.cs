namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Engine;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class SerializeDelegateGeneratorTests : DelegateGeneratorTests
    {
        private readonly List<Type> discoveredTypes = new List<Type>();
        private readonly IFormatter formatter = Substitute.For<IFormatter>();
        private readonly Lazy<SerializeDelegateGenerator> generator;
        private readonly FakeMetadataBuilder metadataBuilder = new FakeMetadataBuilder();

        private SerializeDelegateGeneratorTests()
        {
            this.formatter.Writer.Returns(Substitute.For<ValueWriter>());
            this.generator = new Lazy<SerializeDelegateGenerator>(() =>
                new SerializeDelegateGenerator(
                    new DiscoveredTypes(this.discoveredTypes),
                    this.metadataBuilder));
        }

        private SerializeDelegateGenerator Generator => this.generator.Value;

        public sealed class CreateDelegate : SerializeDelegateGeneratorTests
        {
            [Fact]
            public void ShouldCallTheCustomSerializerForAType()
            {
                lock (CustomSerializer.SyncRoot)
                {
                    this.discoveredTypes.Add(typeof(CustomSerializer));
                    var instance = new PrimitiveProperty();

                    this.Serialize(instance);

                    CustomSerializer.GetLastWritten().Should().BeSameAs(instance);
                }
            }

            [Fact]
            public void ShouldCallWriteClassMethods()
            {
                this.Serialize(new PrimitiveProperty());

                this.formatter.Received().WriteBeginClass(
                    this.metadataBuilder.GetMetadata(nameof(PrimitiveProperty)));

                this.formatter.Received().WriteEndClass();
            }

            [Fact]
            public void ShouldCallWritePrimitiveMethods()
            {
                this.Serialize(123);

                this.formatter.Received().WriteBeginPrimitive(
                    this.metadataBuilder.GetMetadata(nameof(Int32)));

                this.formatter.Received().WriteEndPrimitive();
            }

            [Fact]
            public void ShouldCallWritePropertyMethods()
            {
                this.Serialize(new PrimitiveProperty());

                this.formatter.Received().WriteBeginProperty(
                    this.metadataBuilder.GetMetadata(nameof(PrimitiveProperty.Value)));

                this.formatter.Received().WriteEndProperty();
            }

            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                Action action = () => this.Serialize(new CyclicReference());

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldEnsureThereAreNoMutlipleCustomSerializers()
            {
                this.discoveredTypes.Add(typeof(CustomSerializer));
                this.discoveredTypes.Add(typeof(SecondSerializer));

                Action action = () => this.Serialize(new PrimitiveProperty());

                action.Should().Throw<InvalidOperationException>("*multiple*");
            }

            [Fact]
            public void ShouldEnsureTheTypeIsAReferenceType()
            {
                Action action = () => this.Serialize(new InvalidStruct());

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldNotWriteNonBrowsableProperties()
            {
                this.Serialize(new BrowsableProperties
                {
                    BrowsableFalse = 1,
                    BrowsableTrue = 2
                });

                this.formatter.Writer.DidNotReceive().WriteInt32(1);
                this.formatter.Writer.Received().WriteInt32(2);
            }

            [Fact]
            public void ShouldNotWritePropertiesOfNullableTypesWithoutValues()
            {
                this.Serialize(new NullableProperty { Value = null });

                this.formatter.DidNotReceiveWithAnyArgs().WriteBeginProperty(null);
            }

            [Fact]
            public void ShouldNotWritePropertiesOfReferenceTypesWithoutValues()
            {
                this.Serialize(new ReferenceProperty { Value = null });

                this.formatter.DidNotReceiveWithAnyArgs().WriteBeginProperty(null);
            }

            [Fact]
            public void ShouldWriteEnumsAsStrings()
            {
                this.formatter.EnumsAsIntegers.Returns(false);

                this.Serialize(FakeLongEnum.Value);

                this.formatter.Writer.Received().WriteString(nameof(FakeLongEnum.Value));
            }

            [Fact]
            public void ShouldWriteEnumsAsValues()
            {
                this.formatter.EnumsAsIntegers.Returns(true);

                this.Serialize(FakeLongEnum.Value);

                this.formatter.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldWriteNullableEnums()
            {
                this.formatter.EnumsAsIntegers.Returns(true);

                this.Serialize(typeof(FakeLongEnum?), FakeLongEnum.Value);

                this.formatter.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldWritePrimitiveArrays()
            {
                this.Serialize(new int[] { 123 });

                this.formatter.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldWritePropertiesOfArrayOfEnumElements()
            {
                this.formatter.EnumsAsIntegers.Returns(true);

                this.Serialize(new ArrayProperty { EnumValues = new FakeLongEnum?[] { FakeLongEnum.Value, null } });

                this.formatter.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
                this.formatter.Writer.Received().WriteNull();
            }

            [Fact]
            public void ShouldWritePropertiesOfArrayOfNullablePrimitiveElements()
            {
                this.Serialize(new ArrayProperty { IntValues = new int?[] { null, null } });

                this.formatter.Writer.Received().WriteNull();
                this.formatter.Writer.Received().WriteNull();
            }

            [Fact]
            public void ShouldWritePropertiesOfArrayOfPrimitiveElements()
            {
                this.Serialize(new ArrayProperty { IntValues = new int?[] { 123, 123 } });

                this.formatter.Writer.Received().WriteInt32(123);
                this.formatter.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldWritePropertiesOfEnumNumbers()
            {
                this.formatter.EnumsAsIntegers.Returns(true);

                this.Serialize(new EnumProperty { Value = FakeLongEnum.Value });

                this.formatter.Writer.Received().WriteInt64((long)FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldWritePropertiesOfEnumStrings()
            {
                this.formatter.EnumsAsIntegers.Returns(false);

                this.Serialize(new EnumProperty { Value = FakeLongEnum.Value });

                this.formatter.Writer.Received().WriteString(nameof(FakeLongEnum.Value));
            }

            [Fact]
            public void ShouldWritePropertiesOfNestedTypes()
            {
                this.Serialize(new WithNestedType
                {
                    Nested = new PrimitiveProperty { Value = 123 }
                });

                this.formatter.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldWritePropertiesOfNullableTypesWithValues()
            {
                this.Serialize(new NullableProperty { Value = 123 });

                this.formatter.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldWritePropertiesOfPrimitiveTypes()
            {
                this.Serialize(new PrimitiveProperty { Value = 123 });

                this.formatter.Writer.Received().WriteInt32(123);
            }

            [Fact]
            public void ShouldWritePropertiesOfReferenceTypesWithValues()
            {
                this.Serialize(new ReferenceProperty { Value = "test" });

                this.formatter.Writer.Received().WriteString("test");
            }

            [Fact]
            public void ShouldWritePropertiesOfUnknownTypesAsObject()
            {
                var customValue = new ExampleValueType(123);

                this.Serialize(new ValueProperty { Value = customValue });

                this.formatter.Writer.Received().WriteObject(customValue);
            }

            [Fact]
            public void ShouldWriteToThePrimitiveMethods()
            {
                foreach (MethodInfo method in GetValueWriterMethods())
                {
                    Type parameterType = method.GetParameters().Single().ParameterType;
                    this.formatter.Writer.ClearReceivedCalls();

                    this.Serialize(parameterType, CreateDummyValue(parameterType));

                    this.formatter.Writer.ReceivedCalls().Should().ContainSingle()
                        .Which.GetMethodInfo().Should().BeSameAs(method);
                }
            }

            [Fact]
            public void ShouldWriteToThePrimitiveMethodsForNullableTypes()
            {
                foreach (MethodInfo method in GetValueWriterMethods())
                {
                    Type parameterType = method.GetParameters().Single().ParameterType;
                    if (parameterType.IsValueType)
                    {
                        this.formatter.Writer.ClearReceivedCalls();

                        this.Serialize(
                            typeof(Nullable<>).MakeGenericType(parameterType),
                            CreateDummyValue(parameterType));

                        this.formatter.Writer.ReceivedCalls().Should().ContainSingle()
                            .Which.GetMethodInfo().Should().BeSameAs(method);
                    }
                }
            }

            private static object CreateDummyValue(Type type)
            {
                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                else if (type == typeof(Uri))
                {
                    return new Uri("http://www.example.com");
                }
                else
                {
                    type.Should().Be(typeof(string));
                    return string.Empty;
                }
            }

            private static IEnumerable<MethodInfo> GetValueWriterMethods()
            {
                string[] methodsToIgnore =
                {
                    nameof(ValueWriter.WriteObject),
                    nameof(ValueWriter.WriteNull),
                };

                return typeof(ValueWriter).GetMethods()
                    .Where(m => m.Name.StartsWith("Write") && !methodsToIgnore.Contains(m.Name));
            }

            private void Serialize(Type type, object value)
            {
                SerializeInstance serializer = this.Generator.CreateDelegate(type, this.metadataBuilder);

                serializer(this.formatter, this.metadataBuilder.CreateMetadata<object>(), value);
            }

            private void Serialize<T>(T value)
            {
                this.Serialize(typeof(T), value);
            }
        }

        public sealed class TryGetDelegate : SerializeDelegateGeneratorTests
        {
            [Fact]
            public void ShouldReturnFalseForUnknownTypes()
            {
                bool result = this.Generator.TryGetDelegate(typeof(Unknown), out SerializeInstance serialize);

                result.Should().BeFalse();
                serialize.Should().BeNull();
            }

            private class Unknown
            {
            }
        }
    }
}
