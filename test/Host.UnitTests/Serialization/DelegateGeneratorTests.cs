namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Host.Engine;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    public class DelegateGeneratorTests : DelegateGeneratorClasses
    {
        private readonly List<Type> discoveredTypes = new List<Type>();
        private readonly Lazy<FakeDelegateGenerator> generator;

        private DelegateGeneratorTests()
        {
            this.generator = new Lazy<FakeDelegateGenerator>(() =>
                new FakeDelegateGenerator(
                    new DiscoveredTypes(this.discoveredTypes)));
        }

        private FakeDelegateGenerator Generator => this.generator.Value;

        private Func<Type> CreateDelegateFor<T>()
        {
            return this.Generator.CreateDelegate(typeof(T), null);
        }

        public sealed class CreateCustomSerializer : DelegateGeneratorTests
        {
            [Fact]
            public void ShouldEnsureTheConstructorHasValidArgumentTypes()
            {
                this.discoveredTypes.Add(typeof(SerializerWithInvalidArguments));

                Action action = () => this.Generator.CreateCustomSerializer(typeof(PrimitiveProperty));

                action.Should().Throw<InvalidOperationException>("*ISerializer*");
            }

            [Fact]
            public void ShouldEnsureThereIsASingleConstructor()
            {
                this.discoveredTypes.Add(typeof(SerializerWithMultipleConstructors));

                Action action = () => this.Generator.CreateCustomSerializer(typeof(PrimitiveProperty));

                action.Should().Throw<InvalidOperationException>("*single constructor*");
            }

            [Fact]
            public void ShouldEnsureThereIsOnlyOneCustomSerializerForAType()
            {
                this.discoveredTypes.Add(typeof(CustomPrimitiveSerializer));
                this.discoveredTypes.Add(typeof(SecondSerializer));

                Action action = () => this.Generator.CreateCustomSerializer(typeof(PrimitiveProperty));

                action.Should().Throw<InvalidOperationException>("*multiple*");
            }

            [Fact]
            public void ShouldInjectAGeneratedSerializer()
            {
                lock (CustomNestedSerializer.SyncRoot)
                {
                    this.discoveredTypes.Add(typeof(CustomNestedSerializer));

                    Func<object> result = this.Generator.CreateCustomSerializer(typeof(WithNestedType));
                    object instance = result();

                    instance.Should().BeOfType<CustomNestedSerializer>()
                        .Which.NestedSerializer.Should().NotBeNull();
                }
            }

            [Fact]
            public void ShouldInjectAnotherCustomSerializer()
            {
                lock (CustomNestedSerializer.SyncRoot)
                {
                    this.discoveredTypes.Add(typeof(CustomPrimitiveSerializer));
                    this.discoveredTypes.Add(typeof(CustomNestedSerializer));

                    Func<object> result = this.Generator.CreateCustomSerializer(typeof(WithNestedType));
                    object instance = result();

                    instance.Should().BeOfType<CustomNestedSerializer>()
                        .Which.NestedSerializer.Should().BeOfType<CustomPrimitiveSerializer>();
                }
            }

            [Fact]
            public void ShouldThrowNotSupportedExceptionForTheDelegateAdapter()
            {
                lock (CustomNestedSerializer.SyncRoot)
                {
                    this.discoveredTypes.Add(typeof(CustomNestedSerializer));

                    Func<object> result = this.Generator.CreateCustomSerializer(typeof(WithNestedType));
                    object customSerializer = result();
                    ISerializer<PrimitiveProperty> adapter = ((CustomNestedSerializer)customSerializer).NestedSerializer;

                    adapter.Invoking(a => a.Read(null))
                        .Should().Throw<NotSupportedException>();

                    adapter.Invoking(a => a.Write(null, null))
                        .Should().Throw<NotSupportedException>();
                }
            }
        }

        public sealed class CreateDelegate : DelegateGeneratorTests
        {
            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                Action action = () => this.CreateDelegateFor<CyclicReference>();

                action.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class GetProperties : DelegateGeneratorTests
        {
            [Fact]
            public void ShouldNotReturnPropertiesWithBrowsableFalse()
            {
                IEnumerable<PropertyInfo> result =
                    FakeDelegateGenerator.GetProperties(typeof(BrowsableProperties));

                result.Should().ContainSingle()
                      .Which.Name.Should().Be(nameof(BrowsableProperties.BrowsableTrue));
            }

            [Fact]
            public void ShouldReturnTheOrderedByDataMember()
            {
                List<string> result =
                    FakeDelegateGenerator.GetProperties(typeof(DataMemberProperties))
                    .Select(x => x.Name)
                    .ToList();

                result.Should().Equal("D1", "C2", "A", "B");
            }
        }

        public sealed class TryGetDelegate : DelegateGeneratorTests
        {
            [Fact]
            public void ShouldReturnFalseForUnknownTypes()
            {
                bool result = this.Generator.TryGetDelegate(typeof(Unknown), out Func<Type> @delegate);

                result.Should().BeFalse();
                @delegate.Should().BeNull();
            }

            private class Unknown
            {
            }
        }

        private class FakeDelegateGenerator : DelegateGenerator<Func<Type>>
        {
            public FakeDelegateGenerator(DiscoveredTypes discoveredTypes)
                : base(discoveredTypes)
            {
            }

            internal static new IEnumerable<PropertyInfo> GetProperties(Type type)
            {
                return DelegateGenerator<Func<Type>>.GetProperties(type);
            }

            internal Func<object> CreateCustomSerializer(Type type)
            {
                ParameterExpression metadata = Expression.Parameter(typeof(IReadOnlyList<object>));
                Expression createSerializer = this.CreateCustomSerializer(type, metadata, null);
                return Expression.Lambda<Func<object>>(
                    Expression.Block(
                        new[] { metadata },
                        createSerializer))
                    .Compile();
            }

            protected override Func<Type> BuildDelegate(Type type, MetadataBuilder metadata)
            {
                foreach (PropertyInfo property in GetProperties(type))
                {
                    this.CreateDelegate(property.PropertyType, metadata);
                }

                return () => type;
            }
        }
    }
}
