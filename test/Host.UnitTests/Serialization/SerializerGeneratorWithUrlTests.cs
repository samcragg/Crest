namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization;
    using Xunit;

    [Trait("Category", "Integration")]
    public class SerializerGeneratorWithUrlTests : SerializerGeneratorIntegrationTest<UrlEncodedSerializerBase>
    {
        public sealed class PlainOldDataClasses : SerializerGeneratorWithUrlTests
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
                    @"Enum=0&Integer=0&

                      EnumArray.0=Value&
                      IntegerArray.0=1&
                      NullableIntegerArray.0=1&
                      StringArray.0=string");
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
                    @"Enum=0&Integer=0&

                      Class.Integer=1&
                      ClassArray.0.Integer=2");
            }

            [Fact]
            public void NullProperties()
            {
                this.ShouldSerializeAs(
                    new FullClass { Enum = TestEnum.Value, Integer = 2 },
                    @"Enum=Value&
                      Integer=2");
            }
        }

        public sealed class RootArrays : SerializerGeneratorWithUrlTests
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
                    "0.Integer=1 & 1=null");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    new[] { TestEnum.Value, TestEnum.Value },
                    "0=Value & 1=Value");
            }

            [Fact]
            public void IntegerType()
            {
                this.ShouldSerializeAs(
                    new[] { 1, 2 },
                    "0=1 & 1=2");
            }

            [Fact]
            public void NullableType()
            {
                this.ShouldSerializeAs(
                    new int?[] { 1, null },
                    "0=1 & 1=null");
            }

            [Fact]
            public void StringType()
            {
                this.ShouldSerializeAs(
                    new[] { "string", null },
                    "0=string & 1=null");
            }
        }

        public sealed class RootTypes : SerializerGeneratorWithUrlTests
        {
            [Fact]
            public void ClassType()
            {
                this.ShouldSerializeAs(
                    new SimpleClass { Integer = 1 },
                    "Integer=1");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    TestEnum.Value,
                    "Value");
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
                    "string");
            }
        }
    }
}
