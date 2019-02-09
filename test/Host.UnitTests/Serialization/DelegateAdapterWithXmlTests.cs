namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization.Xml;
    using FluentAssertions;
    using Xunit;

    public class DelegateAdapterWithXmlTests : DelegateAdapterIntegrationTest
    {
        private const string NilAttribute = @"xmlns:i='http://www.w3.org/2001/XMLSchema-instance' i:nil='true'";

        public DelegateAdapterWithXmlTests()
            : base(typeof(XmlFormatter))
        {
        }

        protected override string StripNonEssentialInformation(string result)
        {
            // Strip the <?xml ... ?> part
            result = result.Substring(result.IndexOf("?>") + 2);

            // Strip the namespace used for null values
            return result.Replace(@" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""", string.Empty);
        }

        public sealed class PlainOldDataClassesDeserialize : DelegateAdapterWithXmlTests
        {
            [Fact]
            public void ArrayProperties()
            {
                FullClass result = this.Deserialize<FullClass>(
                    @"<FullClass>
                        <EnumArray><TestEnum>Value</TestEnum></EnumArray>
                        <IntegerArray><int>1</int></IntegerArray>
                        <NullableIntegerArray><int>1</int></NullableIntegerArray>
                        <StringArray><string>string</string></StringArray>
                      </FullClass>");

                result.EnumArray.Should().Equal(TestEnum.Value);
                result.IntegerArray.Should().Equal(1);
                result.NullableIntegerArray.Should().Equal(1);
                result.StringArray.Should().Equal("string");
            }

            [Fact]
            public void NestedClasses()
            {
                FullClass result = this.Deserialize<FullClass>(
                    @"<FullClass>
                        <Class><Integer>1</Integer></Class>
                        <ClassArray><SimpleClass><Integer>2</Integer></SimpleClass></ClassArray>
                      </FullClass>");

                result.Class.Integer.Should().Be(1);
                result.ClassArray.Should().ContainSingle()
                      .Which.Integer.Should().Be(2);
            }
        }

        public sealed class PlainOldDataClassesSerialize : DelegateAdapterWithXmlTests
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

        public sealed class RootArraysDeserialize : DelegateAdapterWithXmlTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass[] result = this.Deserialize<SimpleClass[]>(
                    @"<ArrayOfSimpleClass>
                        <SimpleClass>
                          <Integer>1</Integer>
                        </SimpleClass>
                        <SimpleClass " + NilAttribute + @" />
                      </ArrayOfSimpleClass>");

                result.Should().HaveCount(2);
                result[0].Integer.Should().Be(1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void EmptyArrays()
            {
                int[] result = this.Deserialize<int[]>("<ArrayOfint />");

                result.Should().BeEmpty();
            }

            [Fact]
            public void EnumType()
            {
                TestEnum[] result = this.Deserialize<TestEnum[]>(
                    @"<ArrayOfTestEnum>
                        <TestEnum>Value</TestEnum>
                        <TestEnum>Value</TestEnum>
                      </ArrayOfTestEnum>");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, TestEnum.Value);
                result.Should().HaveElementAt(1, TestEnum.Value);
            }

            [Fact]
            public void IntegerType()
            {
                int[] result = this.Deserialize<int[]>(
                    @"<ArrayOfint>
                        <int>1</int>
                        <int>2</int>
                      </ArrayOfint>");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result.Should().HaveElementAt(1, 2);
            }

            [Fact]
            public void NullableType()
            {
                int?[] result = this.Deserialize<int?[]>(
                    @"<ArrayOfint>
                        <int>1</int>
                        <int " + NilAttribute + @" />
                      </ArrayOfint>");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void StringType()
            {
                string[] result = this.Deserialize<string[]>(
                    @"<ArrayOfstring>
                        <string>string</string>
                        <string " + NilAttribute + @" />
                      </ArrayOfstring>");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, "string");
                result[1].Should().BeNull();
            }
        }

        public sealed class RootArraysSerialize : DelegateAdapterWithXmlTests
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
                    @"<ArrayOfTestEnum>
                        <TestEnum>Value</TestEnum>
                        <TestEnum>Value</TestEnum>
                      </ArrayOfTestEnum>");
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

        public sealed class RootTypesDeserialize : DelegateAdapterWithXmlTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass result = this.Deserialize<SimpleClass>(
                    @"<SimpleClass>
                        <Integer>1</Integer>
                      </SimpleClass>");

                result.Integer.Should().Be(1);
            }

            [Fact]
            public void EnumType()
            {
                TestEnum result = this.Deserialize<TestEnum>("<TestEnum>Value</TestEnum>");

                result.Should().Be(TestEnum.Value);
            }

            [Fact]
            public void EnumNullableType()
            {
                TestEnum? result = this.Deserialize<TestEnum?>("<TestEnum>Value</TestEnum>");

                result.Should().Be(TestEnum.Value);
            }

            [Fact]
            public void IntegerType()
            {
                int result = this.Deserialize<int>("<int>1</int>");

                result.Should().Be(1);
            }

            [Fact]
            public void NullableTypeWithoutValue()
            {
                int? result = this.Deserialize<int?>("<int " + NilAttribute + "/>");

                result.Should().BeNull();
            }

            [Fact]
            public void NullableTypeWithValue()
            {
                int? result = this.Deserialize<int?>("<int>1</int>");

                result.Should().Be(1);
            }

            [Fact]
            public void StringType()
            {
                string result = this.Deserialize<string>("<string>string value</string>");

                result.Should().Be("string value");
            }
        }

        public sealed class RootTypesSerialize : DelegateAdapterWithXmlTests
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
                    "<TestEnum>Value</TestEnum>");
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
