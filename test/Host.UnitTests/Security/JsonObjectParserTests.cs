namespace Host.UnitTests.Security
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using Xunit;

    public class JsonObjectParserTests
    {
        private static JsonObjectParser CreateParser(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return new JsonObjectParser(bytes);
        }

        public sealed class Dispose : JsonObjectParserTests
        {
            [Fact]
            public void ShouldReturnTheStringBuffer()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<char>.Instance.Reset();

                    // Make it allocate some strings
                    JsonObjectParser parser = CreateParser(@"{""key"":123}");
                    parser.GetPairs().ToList();
                    FakeArrayPool<char>.Instance.TotalAllocated.Should().BePositive();

                    // Make sure it returns the bytes
                    parser.Dispose();
                    FakeArrayPool<char>.Instance.TotalAllocated.Should().Be(0);
                }
            }
        }

        public sealed class GetPairs : JsonObjectParserTests
        {
            [Theory]
            [InlineData("[]")]
            [InlineData("[1,2]")]
            [InlineData(@"[""]""]")]
            [InlineData(@"[""\""""]")]
            public void ShouldHandleArrayValues(string value)
            {
                JsonObjectParser parser = CreateParser(@"{""key"":" + value + "}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Value.Should().Be(value);
            }

            [Theory]
            [InlineData(@"{""1"":{""2"":2}}")]
            [InlineData(@"[[1],[2]]")]
            public void ShouldHandleNestedValues(string value)
            {
                JsonObjectParser parser = CreateParser(@"{""key"":" + value + "}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Value.Should().Be(value);
            }

            [Fact]
            public void ShouldHandleNullValues()
            {
                JsonObjectParser parser = CreateParser(@"{""key"":null}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Value.Should().BeNull();
            }

            [Theory]
            [InlineData("{}")]
            [InlineData(@"{""1"":""2""}")]
            [InlineData(@"{""}"":2}")]
            [InlineData(@"{""\"""":""}""}")]
            public void ShouldHandleObjectValues(string value)
            {
                JsonObjectParser parser = CreateParser(@"{""key"":" + value + "}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Value.Should().Be(value);
            }

            [Theory]
            [InlineData("true")]
            [InlineData("123")]
            public void ShouldHandleTokens(string token)
            {
                JsonObjectParser parser = CreateParser(@"{""key"":" + token + "}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Value.Should().Be(token);
            }

            [Fact]
            public void ShouldReturnAllKeyValuePairs()
            {
                JsonObjectParser parser = CreateParser(@"{""1"":""first"",""2"":""second"",""3"":""third""}");

                var result = parser.GetPairs().ToList();

                result.Should().HaveCount(3);
                result.Select(x => x.Key).Should().Equal("1", "2", "3");
            }

            [Fact]
            public void ShouldReturnAnEmptyEnumeratorForEmptyObjects()
            {
                JsonObjectParser parser = CreateParser("{}");

                var result = parser.GetPairs().ToList();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnStringValuesWithoutTheSuroundingQuotations()
            {
                JsonObjectParser parser = CreateParser(@"{""key"":""value""}");

                KeyValuePair<string, string> result = parser.GetPairs().Single();

                result.Key.Should().Be("key");
                result.Value.Should().Be("value");
            }
        }
    }
}
