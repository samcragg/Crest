namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class MetadataBuilderTests
    {
        private readonly MetadataBuilder builder = new MetadataBuilder(0);

        private static PropertyInfo GetProperty(string name)
        {
            return typeof(FakeClass).GetProperty(name);
        }

        public sealed class CreateMetadata : MetadataBuilderTests
        {
            [Fact]
            public void ShouldReturnAllAddedTheProperties()
            {
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property1)));
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property2)));
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property3)));

                IReadOnlyList<object> result = this.builder.CreateMetadata<FakeBaseClass>();

                result.Should().BeEquivalentTo(new object[]
                {
                    nameof(FakeClass.Property1),
                    nameof(FakeClass.Property2),
                    nameof(FakeClass.Property3),
                });
            }

            [Fact]
            public void ShouldReturnAllAddedTheTypes()
            {
                this.builder.GetOrAddMetadata(typeof(FakeBaseClass));
                this.builder.GetOrAddMetadata(typeof(FakeClass));

                IReadOnlyList<object> result = this.builder.CreateMetadata<FakeBaseClass>();

                result.Should().BeEquivalentTo(new object[]
                {
                    nameof(FakeBaseClass),
                    nameof(FakeClass),
                });
            }
        }

        public sealed class GetOrAddMetadata : MetadataBuilderTests
        {
            [Fact]
            public void ShouldReturnTheIndexOfTheExistingProperty()
            {
                int first = this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property1)));
                int second = this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property1)));

                first.Should().Be(second);
            }
        }

        private class FakeBaseClass
        {
            public static string GetMetadata(PropertyInfo property)
            {
                return property.Name;
            }

            public static string GetTypeMetadata(Type type)
            {
                return type.Name;
            }
        }

        private class FakeClass
        {
            public int Property1 { get; set; }
            public int Property2 { get; set; }
            public int Property3 { get; set; }
        }
    }
}
