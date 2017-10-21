namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization;
    using Xunit;

    [Trait("Category", "Integration")]
    public class SerializerGeneratorWithXmlTests : SerializerGeneratorIntegrationTest<XmlSerializerBase>
    {
        protected override string StripNonEssentialInformation(string result)
        {
            // Strip the <?xml ... ?> part
            result = result.Substring(result.IndexOf("?>") + 2);

            // Strip the namespace used for null values
            return result.Replace(@" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""", string.Empty);
        }

        public sealed class PlainOldDataClasses : SerializerGeneratorWithXmlTests
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
                    @"<FullClass>
                        <Enum>0</Enum><Integer>0</Integer>

                        <EnumArray><TestEnum>Value</TestEnum></EnumArray>
                        <IntegerArray><int>1</int></IntegerArray>
                        <NullableIntegerArray><int>1</int></NullableIntegerArray>
                        <StringArray><string>string</string></StringArray>
                      </FullClass>");
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
                    @"<FullClass>
                        <Enum>0</Enum><Integer>0</Integer>

                        <Class><Integer>1</Integer></Class>
                        <ClassArray><SimpleClass><Integer>2</Integer></SimpleClass></ClassArray>
                      </FullClass>");
            }

            [Fact]
            public void NullProperties()
            {
                this.ShouldSerializeAs(
                    new FullClass { Enum = TestEnum.Value, Integer = 2 },
                    @"<FullClass>
                        <Enum>Value</Enum>
                        <Integer>2</Integer>
                      </FullClass>");
            }
        }

        public sealed class RootArrays : SerializerGeneratorWithXmlTests
        {
            [Fact]
            public void ClassType()
            {
                this.ShouldSerializeAs(
                    new[]
                    {
                        new SimpleClass { Integer = 1 },
                        null
                    },
                    @"<ArrayOfSimpleClass>
                        <SimpleClass>
                          <Integer>1</Integer>
                        </SimpleClass>
                        <SimpleClass i:nil=""true"" />
                      </ArrayOfSimpleClass>");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    new[] { TestEnum.Value, TestEnum.Value },
                    @"<ArrayOfEnum>
                        <Enum>Value</Enum>
                        <Enum>Value</Enum>
                      </ArrayOfEnum>");
            }

            [Fact]
            public void IntegerType()
            {
                this.ShouldSerializeAs(
                    new[] { 1, 2 },
                    @"<ArrayOfint>
                        <int>1</int>
                        <int>2</int>
                      </ArrayOfint>");
            }

            [Fact]
            public void NullableType()
            {
                this.ShouldSerializeAs(
                    new int?[] { 1, null },
                    @"<ArrayOfint>
                        <int>1</int>
                        <int i:nil=""true"" />
                      </ArrayOfint>");
            }

            [Fact]
            public void StringType()
            {
                this.ShouldSerializeAs(
                    new[] { "string", null },
                    @"<ArrayOfstring>
                        <string>string</string>
                        <string i:nil=""true"" />
                      </ArrayOfstring>");
            }
        }

        public sealed class RootTypes : SerializerGeneratorWithXmlTests
        {
            [Fact]
            public void ClassType()
            {
                this.ShouldSerializeAs(
                    new SimpleClass { Integer = 1 },
                    @"<SimpleClass>
                        <Integer>1</Integer>
                      </SimpleClass>");
            }

            [Fact]
            public void EnumType()
            {
                this.ShouldSerializeAs(
                    TestEnum.Value,
                    "<Enum>Value</Enum>");
            }

            [Fact]
            public void IntegerType()
            {
                this.ShouldSerializeAs(
                    1,
                    "<int>1</int>");
            }

            [Fact]
            public void NullableType()
            {
                this.ShouldSerializeAs(
                    (int?)1,
                    "<int>1</int>");
            }

            [Fact]
            public void StringType()
            {
                this.ShouldSerializeAs(
                    "string",
                    "<string>string</string>");
            }
        }
    }
}
