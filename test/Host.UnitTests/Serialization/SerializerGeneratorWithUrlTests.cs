namespace Host.UnitTests.Serialization
{
    using System;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    public class SerializerGeneratorWithUrlTests : SerializerGeneratorIntegrationTest<UrlEncodedSerializerBase>
    {
        public sealed class PlainOldDataClassesDeserialize : SerializerGeneratorWithUrlTests
        {
            [Fact]
            public void ArrayProperties()
            {
                FullClass result = this.Deserialize<FullClass>(
                    "EnumArray.0=Value&" +
                    "IntegerArray.0=1&" +
                    "NullableIntegerArray.0=1&" +
                    "StringArray.0=string");

                result.EnumArray.Should().Equal(TestEnum.Value);
                result.IntegerArray.Should().Equal(1);
                result.NullableIntegerArray.Should().Equal(1);
                result.StringArray.Should().Equal("string");
            }

            [Fact]
            public void NestedClasses()
            {
                FullClass result = this.Deserialize<FullClass>(
                    "Class.Integer=1&" +
                    "ClassArray.0.Integer=2");

                result.Class.Integer.Should().Be(1);
                result.ClassArray.Should().ContainSingle()
                      .Which.Integer.Should().Be(2);
            }
        }

        public sealed class PlainOldDataClassesSerialize : SerializerGeneratorWithUrlTests
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
            public void LargeUris()
            {
                string largeString = new string('a', 2000);

                this.ShouldSerializeAs(
                    new FullClass { Uri = new Uri("http://www.example.com/" + largeString) },
                    "Enum=0&Integer=0&" +
                    "Uri=http://www.example.com/" + largeString);
                ;
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

        public sealed class RootArraysDeserialize : SerializerGeneratorWithUrlTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass[] result = this.Deserialize<SimpleClass[]>(
                    "0.Integer=1&1=null");

                result.Should().HaveCount(2);
                result[0].Integer.Should().Be(1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void EmptyArrays()
            {
                int[] result = this.Deserialize<int[]>("");

                result.Should().BeEmpty();
            }

            [Fact]
            public void EnumType()
            {
                TestEnum[] result = this.Deserialize<TestEnum[]>(
                    "0=Value&1=Value");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, TestEnum.Value);
                result.Should().HaveElementAt(1, TestEnum.Value);
            }

            [Fact]
            public void IntegerType()
            {
                int[] result = this.Deserialize<int[]>(
                    "0=1&1=2");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result.Should().HaveElementAt(1, 2);
            }

            [Fact]
            public void NullableType()
            {
                int?[] result = this.Deserialize<int?[]>(
                    "0=1&1=null");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, 1);
                result[1].Should().BeNull();
            }

            [Fact]
            public void StringType()
            {
                string[] result = this.Deserialize<string[]>(
                    "0=string&1=null");

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, "string");
                result[1].Should().BeNull();
            }
        }

        public sealed class RootArraysSerialize : SerializerGeneratorWithUrlTests
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

        public sealed class RootTypesDeserialize : SerializerGeneratorWithUrlTests
        {
            [Fact]
            public void ClassType()
            {
                SimpleClass result = this.Deserialize<SimpleClass>(
                    "Integer=1");

                result.Integer.Should().Be(1);
            }

            [Fact]
            public void EnumType()
            {
                TestEnum result = this.Deserialize<TestEnum>("Value");

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
                string result = this.Deserialize<string>("string");

                result.Should().Be("string");
            }
        }

        public sealed class RootTypesSerialize : SerializerGeneratorWithUrlTests
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
