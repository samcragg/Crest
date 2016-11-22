namespace Host.AspNetCore.UnitTests
{
    using System.Collections;
    using System.Collections.Generic;
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HeadersAdapterTests
    {
        private IHeaderDictionary headers;
        private HeadersAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            this.headers = Substitute.For<IHeaderDictionary>();
            this.adapter = new HeadersAdapter(this.headers);
        }

        [Test]
        public void CountShouldReturnTheNumberOfHeaders()
        {
            this.headers.Count.Returns(12);

            Assert.That(this.adapter.Count, Is.EqualTo(12));
        }

        [Test]
        public void KeysShouldReturnTheHeaderFields()
        {
            this.headers.Keys.Returns(new[] { "1", "2" });

            Assert.That(this.adapter.Keys, Is.EquivalentTo(new[] { "1", "2" }));
        }

        [Test]
        public void ValuesShouldReturnTheJoinedValues()
        {
            this.headers.Values.Returns(new[]
            {
                new StringValues("single"),
                new StringValues(new[] { "1", "2" })
            });

            Assert.That(this.adapter.Values, Is.EquivalentTo(new[] { "single", "1,2" }));
        }

        [Test]
        public void IndexShouldReturnTheValue()
        {
            StringValues any = Arg.Any<StringValues>();
            this.headers.TryGetValue("key", out any)
                .Returns(ci =>
                {
                    ci[1] = new StringValues("value");
                    return true;
                });

            string result = this.adapter["key"];

            Assert.That(result, Is.EqualTo("value"));
        }

        [Test]
        public void IndexShouldThrowAnExceptionIfTheHeaderDoesNotExist()
        {
            Assert.That(
                () => this.adapter["unknown"],
                Throws.InstanceOf<KeyNotFoundException>());
        }

        [Test]
        public void ContainsKeyShouldReturnTrueIfTheHeaderExists()
        {
            this.headers.ContainsKey("key").Returns(true);

            Assert.That(this.adapter.ContainsKey("key"), Is.True);
        }

        [Test]
        public void ContainsKeyShouldReturnFalseIfTheHeaderDoesNotExist()
        {
            Assert.That(this.adapter.ContainsKey("unknown"), Is.False);
        }

        [Test]
        public void GetEnumeratorShouldIterateOverAllTheKeyValues()
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

            Assert.That(results, Is.EquivalentTo(new[] { "key1", "value1", "key2", "value2" }));
        }

        [Test]
        public void TryGetValueShouldReturnTrueIfTheHeaderExists()
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

            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public void TryGetValueShouldReturnFalseIfTheHeaderDoesNotExist()
        {
            string value;
            bool result = this.adapter.TryGetValue("unknown", out value);

            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void NonGenericGetEnumeratorShouldGetAllTheValues()
        {
            // Array.GetEnumerator returns the non-generic one
            this.headers.GetEnumerator().Returns(new List<KeyValuePair<string, StringValues>>
            {
                new KeyValuePair<string, StringValues>("key1", "value1"),
                new KeyValuePair<string, StringValues>("key2", "value2"),
            }.GetEnumerator());

            IEnumerator enumerator = ((IEnumerable)this.adapter).GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);
        }
    }
}
