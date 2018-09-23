namespace DataAccess.UnitTests.Parsing
{
    using System;
    using System.Linq;
    using Crest.DataAccess.Parsing;
    using FluentAssertions;
    using Xunit;

    public class CsvSplitterTests
    {
        private readonly CsvSplitter splitter = new CsvSplitter();

        public sealed class Split : CsvSplitterTests
        {
            [Fact]
            public void ShouldAllowCommasInQuotedValues()
            {
                string[] result = this.splitter.Split("a,\"b,c\",d").ToArray();

                result.Should().Equal("a", "b,c", "d");
            }

            [Fact]
            public void ShouldSplitOnCommas()
            {
                string[] result = this.splitter.Split("a,b,c").ToArray();

                result.Should().Equal("a", "b", "c");
            }

            [Fact]
            public void ShouldThrowForInvalidQuotedValues()
            {
                Action action = () => this.splitter.Split("a,\"b").ToArray();

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldUnescapeQuotedValues()
            {
                string[] result = this.splitter.Split("a,\"b\"\"c\"").ToArray();

                result.Should().Equal("a", "b\"c");
            }
        }
    }
}
