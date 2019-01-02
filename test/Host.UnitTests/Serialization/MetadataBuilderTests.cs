namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class MetadataBuilderTests
    {
        private readonly MetadataBuilder builder;

        public MetadataBuilderTests()
        {
            MethodInfo method = typeof(MetadataBuilderTests).GetMethod(nameof(MetadataMethod));
            this.builder = new MetadataBuilder(method);
        }

        public static string MetadataMethod(PropertyInfo property)
        {
            return property.Name;
        }

        private static PropertyInfo GetProperty(string name)
        {
            return typeof(FakeClass).GetProperty(name);
        }

        public sealed class GetMetadataArray : MetadataBuilderTests
        {
            [Fact]
            public void ShouldReturnAllAddedTheMetadata()
            {
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property1)));
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property2)));
                this.builder.GetOrAddMetadata(GetProperty(nameof(FakeClass.Property3)));

                Array result = this.builder.GetMetadataArray(typeof(string));

                result.Should().BeEquivalentTo(new object[]
                {
                    nameof(FakeClass.Property1),
                    nameof(FakeClass.Property2),
                    nameof(FakeClass.Property3),
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

            [Fact]
            public void ShouldReturnTheIndexOfTheExistingType()
            {
                int callcount = 0;
                int first = this.builder.GetOrAddMetadata(typeof(FakeClass), () => callcount++);
                int second = this.builder.GetOrAddMetadata(typeof(FakeClass), () => callcount++);

                first.Should().Be(second);
                callcount.Should().Be(1);
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
