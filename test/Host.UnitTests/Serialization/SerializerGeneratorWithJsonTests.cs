namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization;
    using Xunit;

    [Trait("Category", "Integration")]
    public class SerializerGeneratorWithJsonTests : SerializerGeneratorIntegrationTestBase
    {
        private SerializerGenerator generator = new SerializerGenerator(typeof(JsonSerializerBase));

        internal override SerializerGenerator Generator => this.generator;

        public sealed class PlainOldDataClasses : SerializerGeneratorWithJsonTests
        {
            [Fact]
            public void ArrayProperties()
            {
                this.ShouldSerializeAs(
                    new FullClass
                    {
                        EnumArray = new[] { TestEnum.Value },
                        IntegerArray = new[] { 1 },
                        NullableIntegerArray = new int?[] { 1 },
                        StringArray = new[] { "string" }
                    },
                    @"{ ""enumArray"": [1],
                        ""integerArray"": [1],
                        ""nullableIntegerArray"": [1],
                        ""stringArray"": [""string""] }");
            }

            [Fact]
            public void NestedClasses()
            {
                this.ShouldSerializeAs(
                    new FullClass
                    {
                        Class = new SimpleClass { Integer = 1 },
                        ClassArray = new[] { new SimpleClass { Integer = 2 } }
                    },
                    @"{ ""class"": { ""integer"": 1 },
                        ""classArray"": [{ ""integer"": 2 }] }");
            }

            [Fact]
            public void NullProperties()
            {
                this.ShouldSerializeAs(
                    new FullClass { Enum = TestEnum.Value, Integer = 2 },
                    "{ \"enum\":1, \"integer\":2 }");
            }

            protected override string StripNonEssentialInformation(string result)
            {
                // The integer and enum properties will always be serializer,
                // so strip them if they have their default values
                return result.Replace("\"enum\":0,\"integer\":0,", string.Empty);
            }
        }

        public sealed class RootArrays : SerializerGeneratorWithJsonTests
        {
            [Fact]
            public void ClassType()
            {
                this.ShouldSerializeAs(
                    new[]
                    {
                        new SimpleClass{ Integer = 1 },
                        null
                    },
                    "[ { \"integer\": 1 }, null ]");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    new[] { TestEnum.Value, TestEnum.Value },
                    "[ 1, 1 ]");
            }

            [Fact]
            public void IntegerType()
            {
                this.ShouldSerializeAs(
                    new[] { 1, 2 },
                    "[ 1, 2 ]");
            }

            [Fact]
            public void NullableType()
            {
                this.ShouldSerializeAs(
                    new int?[] { 1, null },
                    "[ 1, null ]");
            }

            [Fact]
            public void StringType()
            {
                this.ShouldSerializeAs(
                    new[] { "string", null },
                    "[ \"string\", null ]");
            }
        }

        public sealed class RootTypes : SerializerGeneratorWithJsonTests
        {
            [Fact]
            public void ClassType()
            {
                this.ShouldSerializeAs(
                    new SimpleClass { Integer = 1 },
                    "{ \"integer\": 1 }");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    TestEnum.Value,
                    "1");
            }

            [Fact]
            public void IntegerType()
            {
                this.ShouldSerializeAs(
                    1,
                    "1");
            }

            [Fact]
            public void NullableType()
            {
                this.ShouldSerializeAs(
                    (int?)1,
                    "1");
            }

            [Fact]
            public void StringType()
            {
                this.ShouldSerializeAs(
                    "string",
                    "\"string\"");
            }
        }
    }
}
