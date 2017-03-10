namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using NSubstitute;
    using Xunit;

    public class HeadersAdapterTests
    {
        private HeadersAdapter adapter;
        private IHeaderDictionary headers;

        public HeadersAdapterTests()
        {
            this.headers = Substitute.For<IHeaderDictionary>();
            this.adapter = new HeadersAdapter(this.headers);
        }

        public sealed class ContainsKey : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheHeaderDoesNotExist()
            {
                this.adapter.ContainsKey("unknown").Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheHeaderExists()
            {
                this.headers.ContainsKey("key").Returns(true);

                this.adapter.ContainsKey("key").Should().BeTrue();
            }
        }

        public sealed class Count : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnTheNumberOfHeaders()
            {
                this.headers.Count.Returns(12);

                this.adapter.Count.Should().Be(12);
            }
        }

        public sealed class GetEnumerator : HeadersAdapterTests
        {
            [Fact]
            public void ShouldIterateOverAllTheKeyValues()
            {
                // Array.GetEnumerator returns the non-generic one
                this.headers.GetEnumerator().Returns(new List<KeyValuePair<string, StringValues>>
                {
                    new KeyValuePair<string, StringValues>("key1", "value1"),
                    new KeyValuePair<string, StringValues>("key2", "value2"),
                }.GetEnumerator());

                var results = new List<string>();
                foreach (KeyValuePair<string, string> kvp in this.adapter)
                {
                    results.Add(kvp.Key);
                    results.Add(kvp.Value);
                }

                results.Should().BeEquivalentTo(new[] { "key1", "value1", "key2", "value2" });
            }
        }

        public sealed class Index : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnTheValue()
            {
                StringValues any = Arg.Any<StringValues>();
                this.headers.TryGetValue("key", out any)
                    .Returns(ci =>
                    {
                        ci[1] = new StringValues("value");
                        return true;
                    });

                string result = this.adapter["key"];

                result.Should().Be("value");
            }

            [Fact]
            public void ShouldThrowAnExceptionIfTheHeaderDoesNotExist()
            {
                Action action = () => { var x = this.adapter["unknown"]; };

                action.ShouldThrow<KeyNotFoundException>();
            }
        }

        public sealed class Keys : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnTheHeaderFields()
            {
                this.headers.Keys.Returns(new[] { "1", "2" });

                this.adapter.Keys.Should().BeEquivalentTo(new[] { "1", "2" });
            }
        }

        public sealed class NonGenericGetEnumerator : HeadersAdapterTests
        {
            [Fact]
            public void ShouldGetAllTheValues()
            {
                // Array.GetEnumerator returns the non-generic one
                this.headers.GetEnumerator().Returns(new List<KeyValuePair<string, StringValues>>
                {
                    new KeyValuePair<string, StringValues>("key1", "value1"),
                    new KeyValuePair<string, StringValues>("key2", "value2"),
                }.GetEnumerator());

                IEnumerator enumerator = ((IEnumerable)this.adapter).GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeFalse();
            }
        }

        public sealed class TryGetValue : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheHeaderDoesNotExist()
            {
                string value;
                bool result = this.adapter.TryGetValue("unknown", out value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTrueIfTheHeaderExists()
            {
                StringValues any = Arg.Any<StringValues>();
                this.headers.TryGetValue("key", out any)
                    .Returns(ci =>
                    {
                        ci[1] = new StringValues("value");
                        return true;
                    });

                string value;
                bool result = this.adapter.TryGetValue("key", out value);

                result.Should().BeTrue();
                value.Should().Be("value");
            }
        }

        public sealed class Values : HeadersAdapterTests
        {
            [Fact]
            public void ShouldReturnTheJoinedValues()
            {
                this.headers.Values.Returns(new[]
                {
                    new StringValues("single"),
                    new StringValues(new[] { "1", "2" })
                });

                this.adapter.Values.Should().BeEquivalentTo(new[] { "single", "1,2" });
            }
        }
    }
}
