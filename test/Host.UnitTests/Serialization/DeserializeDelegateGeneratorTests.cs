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
    using NSubstitute.ReturnsExtensions;
    using Xunit;

    public class DeserializeDelegateGeneratorTests : DelegateGeneratorTests
    {
        private readonly List<Type> discoveredTypes = new List<Type>();
        private readonly IFormatter formatter = Substitute.For<IFormatter>();
        private readonly Lazy<DeserializeDelegateGenerator> generator;
        private readonly FakeMetadataBuilder metadataBuilder = new FakeMetadataBuilder();

        private DeserializeDelegateGeneratorTests()
        {
            this.formatter.Reader.Returns(Substitute.For<ValueReader>());
            this.formatter.ReadBeginProperty().ReturnsNull();
            this.generator = new Lazy<DeserializeDelegateGenerator>(() =>
                new DeserializeDelegateGenerator(
                    new DiscoveredTypes(this.discoveredTypes),
                    this.metadataBuilder));
        }

        private DeserializeDelegateGenerator Generator => this.generator.Value;

        private void SetProperties(params string[] parameters)
        {
            string[] remaining = parameters.Append(null).Skip(1).ToArray();
            this.formatter.ReadBeginProperty().Returns(parameters[0], remaining);
        }

        public sealed class CreateDelegate : DeserializeDelegateGeneratorTests
        {
            [Fact]
            public void ShouldCallReadClassMethods()
            {
                this.Deserialize<PrimitiveProperty>();

                this.formatter.Received().ReadBeginClass(
                    this.metadataBuilder.GetMetadata(nameof(PrimitiveProperty)));

                this.formatter.Received().ReadEndClass();
            }

            [Fact]
            public void ShouldCallReadPrimitiveMethods()
            {
                this.Deserialize<int>();

                this.formatter.Received().ReadBeginPrimitive(
                    this.metadataBuilder.GetMetadata(nameof(Int32)));

                this.formatter.Received().ReadEndPrimitive();
            }

            [Fact]
            public void ShouldCallReadPropertyMethods()
            {
                this.SetProperties(nameof(PrimitiveProperty.Value));

                this.Deserialize<PrimitiveProperty>();

                this.formatter.Received().ReadBeginProperty();
                this.formatter.Received().ReadEndProperty();
            }

            [Fact]
            public void ShouldCallTheCustomSerializerForAType()
            {
                lock (CustomSerializer.SyncRoot)
                {
                    this.discoveredTypes.Add(typeof(CustomSerializer));
                    var instance = new PrimitiveProperty();
                    CustomSerializer.SetNextRead(instance);

                    PrimitiveProperty result = this.Deserialize<PrimitiveProperty>();

                    result.Should().BeSameAs(instance);
                }
            }

            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                Action action = () => this.Deserialize<CyclicReference>();

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldEnsureThereAreNoMutlipleCustomSerializers()
            {
                this.discoveredTypes.Add(typeof(CustomSerializer));
                this.discoveredTypes.Add(typeof(SecondSerializer));

                Action action = () => this.Deserialize<PrimitiveProperty>();

                action.Should().Throw<InvalidOperationException>("*multiple*");
            }

            [Fact]
            public void ShouldEnsureTheTypeIsAReferenceType()
            {
                Action action = () => this.Deserialize<InvalidStruct>();

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldNotReadNonBrowsableProperties()
            {
                this.formatter.Reader.ReadInt32().Returns(1);
                this.SetProperties(
                    nameof(BrowsableProperties.BrowsableFalse),
                    nameof(BrowsableProperties.BrowsableTrue));

                BrowsableProperties result = this.Deserialize<BrowsableProperties>();

                result.BrowsableFalse.Should().Be(0);
                result.BrowsableTrue.Should().Be(1);
            }

            [Fact]
            public void ShouldNotReadPropertiesOfNullableTypesWithoutValues()
            {
                this.formatter.Reader.ReadNull().Returns(false, true);
                this.SetProperties(nameof(NullableProperty.Value));

                NullableProperty result = this.Deserialize<NullableProperty>();

                result.Value.Should().BeNull();
                this.formatter.Reader.DidNotReceive().ReadInt32();
            }

            [Fact]
            public void ShouldNotReadPropertiesOfReferenceTypesWithoutValues()
            {
                this.formatter.Reader.ReadNull().Returns(false, true);
                this.SetProperties(nameof(ReferenceProperty.Value));

                ReferenceProperty result = this.Deserialize<ReferenceProperty>();

                result.Value.Should().BeNull();
                this.formatter.Reader.DidNotReceive().ReadString();
            }

            [Fact]
            public void ShouldReadEnumsAsIntegers()
            {
                this.formatter.EnumsAsIntegers.Returns(true);
                this.formatter.Reader.ReadInt64().Returns((long)FakeLongEnum.Value);

                FakeLongEnum result = this.Deserialize<FakeLongEnum>();

                result.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadEnumsAsStrings()
            {
                this.formatter.EnumsAsIntegers.Returns(false);
                this.formatter.Reader.ReadString().Returns(nameof(FakeLongEnum.Value));

                FakeLongEnum result = this.Deserialize<FakeLongEnum>();

                result.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadFromThePrimitiveMethods()
            {
                this.formatter.Reader.ReadNull().Returns(false);

                foreach (MethodInfo method in GetValueReaderMethods())
                {
                    this.formatter.Reader.ClearReceivedCalls();

                    this.Deserialize(method.ReturnType);

                    this.formatter.Reader.ReceivedCalls()
                            .Last().GetMethodInfo().Should().BeSameAs(method);
                }
            }

            [Fact]
            public void ShouldReadFromThePrimitiveMethodsForNullableTypes()
            {
                this.formatter.Reader.ReadNull().Returns(false);

                foreach (MethodInfo method in GetValueReaderMethods())
                {
                    Type orimitiveType = method.ReturnType;
                    if (orimitiveType.IsValueType)
                    {
                        this.formatter.Reader.ClearReceivedCalls();

                        this.Deserialize(typeof(Nullable<>).MakeGenericType(orimitiveType));

                        this.formatter.Reader.ReceivedCalls()
                            .Last().GetMethodInfo().Should().BeSameAs(method);
                    }
                }
            }

            [Fact]
            public void ShouldReadNullableEnums()
            {
                this.formatter.EnumsAsIntegers.Returns(true);
                this.formatter.Reader.ReadNull().Returns(false);
                this.formatter.Reader.ReadInt64().Returns((long)FakeLongEnum.Value);

                FakeLongEnum? result = this.Deserialize<FakeLongEnum?>();

                result.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadNullObjects()
            {
                this.formatter.Reader.ReadNull().Returns(true);

                PrimitiveProperty result = this.Deserialize<PrimitiveProperty>();

                result.Should().BeNull();
                this.formatter.DidNotReceiveWithAnyArgs().ReadBeginClass(null);
            }

            [Fact]
            public void ShouldReadPrimitiveArrays()
            {
                this.formatter.ReadBeginArray(typeof(int)).Returns(true);
                this.formatter.Reader.ReadInt32().Returns(123);

                int[] result = this.Deserialize<int[]>();

                result.Should().Equal(123);
            }

            [Fact]
            public void ShouldReadPropertiesOfArrayOfEnumElements()
            {
                this.formatter.EnumsAsIntegers.Returns(true);
                this.formatter.ReadBeginArray(typeof(FakeLongEnum?)).Returns(true);
                this.formatter.ReadElementSeparator().Returns(true, false);
                this.formatter.Reader.ReadInt64().Returns((long)FakeLongEnum.Value);
                this.formatter.Reader.ReadNull().Returns(false, false, true);
                this.SetProperties(nameof(ArrayProperty.EnumValues));

                ArrayProperty result = this.Deserialize<ArrayProperty>();

                result.EnumValues.Should().Equal(FakeLongEnum.Value, null);
            }

            [Fact]
            public void ShouldReadPropertiesOfArrayOfPrimitiveElements()
            {
                this.formatter.ReadBeginArray(typeof(int?)).Returns(true);
                this.formatter.ReadElementSeparator().Returns(true, false);
                this.formatter.Reader.ReadInt32().Returns(123);
                this.formatter.Reader.ReadNull().Returns(false, false, true);
                this.SetProperties(nameof(ArrayProperty.IntValues));

                ArrayProperty result = this.Deserialize<ArrayProperty>();

                result.IntValues.Should().Equal(123, null);
            }

            [Fact]
            public void ShouldReadPropertiesOfEnumNumbers()
            {
                this.formatter.EnumsAsIntegers.Returns(true);
                this.formatter.Reader.ReadInt64().Returns((long)FakeLongEnum.Value);
                this.SetProperties(nameof(EnumProperty.Value));

                EnumProperty result = this.Deserialize<EnumProperty>();

                result.Value.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadPropertiesOfEnumStrings()
            {
                this.formatter.EnumsAsIntegers.Returns(false);
                this.formatter.Reader.ReadString().Returns(nameof(FakeLongEnum.Value));
                this.SetProperties(nameof(EnumProperty.Value));

                EnumProperty result = this.Deserialize<EnumProperty>();

                result.Value.Should().Be(FakeLongEnum.Value);
            }

            [Fact]
            public void ShouldReadPropertiesOfNestedTypes()
            {
                this.formatter.Reader.ReadInt32().Returns(123);
                this.SetProperties(nameof(WithNestedType.Nested), nameof(PrimitiveProperty.Value));

                WithNestedType result = this.Deserialize<WithNestedType>();

                result.Nested.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReadPropertiesOfNullableTypesWithValues()
            {
                this.formatter.Reader.ReadInt32().Returns(123);
                this.formatter.Reader.ReadNull().Returns(false);
                this.SetProperties(nameof(NullableProperty.Value));

                NullableProperty result = this.Deserialize<NullableProperty>();

                result.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReadPropertiesOfPrimitiveTypes()
            {
                this.formatter.Reader.ReadInt32().Returns(123);
                this.SetProperties(nameof(PrimitiveProperty.Value));

                PrimitiveProperty result = this.Deserialize<PrimitiveProperty>();

                result.Value.Should().Be(123);
            }

            [Fact]
            public void ShouldReadPropertiesOfReferenceTypesWithValues()
            {
                this.formatter.Reader.ReadNull().Returns(false);
                this.formatter.Reader.ReadString().Returns("test");
                this.SetProperties(nameof(ReferenceProperty.Value));

                ReferenceProperty result = this.Deserialize<ReferenceProperty>();

                result.Value.Should().Be("test");
            }

            [Fact]
            public void ShouldReadPropertiesOfUnknownTypesAsObject()
            {
                this.formatter.Reader.ReadNull().Returns(false);
                this.formatter.Reader.ReadObject(typeof(ExampleValueType))
                    .Returns(new ExampleValueType(123));
                this.SetProperties(nameof(ValueProperty.Value));

                ValueProperty result = this.Deserialize<ValueProperty>();

                result.Value.Value.Should().Be(123);
            }

            private static IEnumerable<MethodInfo> GetValueReaderMethods()
            {
                string[] methodsToIgnore =
                {
                    nameof(ValueReader.ReadObject),
                    nameof(ValueReader.ReadNull),
                };

                return typeof(ValueReader).GetMethods()
                    .Where(m => m.Name.StartsWith("Read") && !methodsToIgnore.Contains(m.Name));
            }

            private object Deserialize(Type type)
            {
                DeserializeInstance serializer = this.Generator.CreateDelegate(type, this.metadataBuilder);

                return serializer(this.formatter, this.metadataBuilder.CreateMetadata<object>());
            }

            private T Deserialize<T>()
            {
                return (T)this.Deserialize(typeof(T));
            }
        }

        public sealed class TryGetDelegate : DeserializeDelegateGeneratorTests
        {
            [Fact]
            public void ShouldReturnFalseForUnknownTypes()
            {
                bool result = this.Generator.TryGetDelegate(typeof(Unknown), out DeserializeInstance deserialize);

                result.Should().BeFalse();
                deserialize.Should().BeNull();
            }

            private class Unknown
            {
            }
        }
    }
}
