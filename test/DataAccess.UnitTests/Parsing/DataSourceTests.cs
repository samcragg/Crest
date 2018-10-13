namespace DataAccess.UnitTests.Parsing
{
    using System.Collections.Generic;
    using System.Dynamic;
    using Crest.DataAccess.Parsing;
    using FluentAssertions;
    using Xunit;

    public class DataSourceTests
    {
        public sealed class GetMembers : DataSourceTests
        {
            [Fact]
            public void ShouldReturnTheDynamicMembers()
            {
                dynamic expando = new ExpandoObject();
                expando.member = "";
                var source = new DataSource(expando);

                IEnumerable<string> result = source.GetMembers();

                result.Should().Equal("member");
            }

            [Fact]
            public void ShouldReturnTheKeysForDictionaries()
            {
                var source = new DataSource(new Dictionary<string, string[]>
                {
                    { "key", new[] { "value" } },
                });

                IEnumerable<string> result = source.GetMembers();

                result.Should().Equal("key");
            }

            [Fact]
            public void ShouldReturnTheMembersOfAnonymousTypes()
            {
                var source = new DataSource(new { member = "" });

                IEnumerable<string> result = source.GetMembers();

                result.Should().Equal("member");
            }
        }

        public sealed class GetValue : DataSourceTests
        {
            [Fact]
            public void ShouldReturnTheValueFromAnonymousTypes()
            {
                var source = new DataSource(new { member = "value" });

                object result = source.GetValue("member");

                result.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnTheValueFromDictionaries()
            {
                string[] values = { "value" };
                var source = new DataSource(new Dictionary<string, string[]>
                {
                    { "key",  values },
                });

                object result = source.GetValue("key");

                result.Should().BeSameAs(values);
            }

            [Fact]
            public void ShouldReturnTheValueFromDynamicTypes()
            {
                dynamic expando = new ExpandoObject();
                expando.member = "value";
                var source = new DataSource(expando);

                object result = source.GetValue("member");

                result.Should().Be("value");
            }
        }
    }
}
