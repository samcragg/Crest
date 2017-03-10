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
    using NUnit.Framework;

    [TestFixture]
    public class HeadersAdapterTests
    {
        private HeadersAdapter adapter;
        private IHeaderDictionary headers;

        [SetUp]
        public void SetUp()
        {
            this.headers = Substitute.For<IHeaderDictionary>();
            this.adapter = new HeadersAdapter(this.headers);
        }

        [TestFixture]
        public sealed class ContainsKey : HeadersAdapterTests
        {
            [Test]
            public void ShouldReturnFalseIfTheHeaderDoesNotExist()
            {
                this.adapter.ContainsKey("unknown").Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueIfTheHeaderExists()
            {
                this.headers.ContainsKey("key").Returns(true);

                this.adapter.ContainsKey("key").Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Count : HeadersAdapterTests
        {
            [Test]
            public void ShouldReturnTheNumberOfHeaders()
            {
                this.headers.Count.Returns(12);

                this.adapter.Count.Should().Be(12);
            }
        }

        [TestFixture]
        public sealed class GetEnumerator : HeadersAdapterTests
        {
            [Test]
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

        [TestFixture]
        public sealed class Index : HeadersAdapterTests
        {
            [Test]
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

            [Test]
            public void ShouldThrowAnExceptionIfTheHeaderDoesNotExist()
            {
                Action action = () => { var x = this.adapter["unknown"]; };

                action.ShouldThrow<KeyNotFoundException>();
            }
        }

        [TestFixture]
        public sealed class Keys : HeadersAdapterTests
        {
            [Test]
            public void ShouldReturnTheHeaderFields()
            {
                this.headers.Keys.Returns(new[] { "1", "2" });

                this.adapter.Keys.Should().BeEquivalentTo(new[] { "1", "2" });
            }
        }

        [TestFixture]
        public sealed class NonGenericGetEnumerator : HeadersAdapterTests
        {
            [Test]
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

        [TestFixture]
        public sealed class TryGetValue : HeadersAdapterTests
        {
            [Test]
            public void ShouldReturnFalseIfTheHeaderDoesNotExist()
            {
                string value;
                bool result = this.adapter.TryGetValue("unknown", out value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Test]
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

        [TestFixture]
        public sealed class Values : HeadersAdapterTests
        {
            [Test]
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
