namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization.Json;
    using FluentAssertions;
    using Xunit;

    public class FormatterSerializerWithJsonTests : FormatterSerializerIntegrationTest
    {
        public FormatterSerializerWithJsonTests()
            : base(typeof(JsonFormatter))
        {
        }

        public sealed class PlainOldDataClassesDeserialize : FormatterSerializerWithJsonTests
        {
            [Fact]
            public void ArrayProperties()
            {
                FullClass result = this.Deserialize<FullClass>(
                    @"{ ""enumArray"": [1],
                        ""integerArray"": [1],
                        ""nullableIntegerArray"": [1],
                        ""stringArray"": [""string""] }");

                result.EnumArray.Should().Equal(TestEnum.Value);
                result.IntegerArray.Should().Equal(1);
                result.NullableIntegerArray.Should().Equal(1);
                result.StringArray.Should().Equal("string");
            }

            [Fact]
            public void NestedClasses()
            {
                FullClass result = this.Deserialize<FullClass>(
                    @"{ ""class"": { ""integer"": 1 },
                        ""classArray"": [{ ""integer"": 2 }] }");

                result.Class.Integer.Should().Be(1);
                result.ClassArray.Should().ContainSingle()
                      .Which.Integer.Should().Be(2);
            }
        }

        public sealed class PlainOldDataClassesSerialize : FormatterSerializerWithJsonTests
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

        public sealed class RootArraysDeserialize : FormatterSerializerWithJsonTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass[] result = this.Deserialize<SimpleClass[]>(
                    "[ { \"integer\": 1 }, null ]");

                result.Should().HaveCount(2);
                result[0].Integer.Should().Be(1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void EmptyArrays()
            {
                int[] result = this.Deserialize<int[]>("[]");

                result.Should().BeEmpty();
            }

            [Fact]
            public void EnumType()
            {
                TestEnum[] result = this.Deserialize<TestEnum[]>(
                    "[ 1, 1 ]");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, TestEnum.Value);
                result.Should().HaveElementAt(1, TestEnum.Value);
            }

            [Fact]
            public void IntegerType()
            {
                int[] result = this.Deserialize<int[]>(
                    "[ 1, 2 ]");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result.Should().HaveElementAt(1, 2);
            }

            [Fact]
            public void NullableType()
            {
                int?[] result = this.Deserialize<int?[]>(
                    "[ 1, null ]");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void StringType()
            {
                string[] result = this.Deserialize<string[]>(
                    "[ \"string\", null ]");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, "string");
                result[1].Should().BeNull();
            }
        }

        public sealed class RootArraysSerialize : FormatterSerializerWithJsonTests
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

        public sealed class RootTypesDeserialize : FormatterSerializerWithJsonTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass result = this.Deserialize<SimpleClass>(
                    "{ \"integer\": 1 }");

                result.Integer.Should().Be(1);
            }

            [Fact]
            public void EnumType()
            {
                TestEnum result = this.Deserialize<TestEnum>("1");

                result.Should().Be(TestEnum.Value);
            }

            [Fact]
            public void IntegerType()
            {
                int result = this.Deserialize<int>("1");

                result.Should().Be(1);
            }

            [Fact]
            public void NullableTypeWithoutValue()
            {
                int? result = this.Deserialize<int?>("null");

                result.Should().BeNull();
            }

            [Fact]
            public void NullableTypeWithValue()
            {
                int? result = this.Deserialize<int?>("1");

                result.Should().Be(1);
            }

            [Fact]
            public void StringType()
            {
                string result = this.Deserialize<string>("\"string\"");

                result.Should().Be("string");
            }
        }

        public sealed class RootTypesSerialize : FormatterSerializerWithJsonTests
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
