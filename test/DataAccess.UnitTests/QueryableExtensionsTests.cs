namespace DataAccess.UnitTests
{
    using System;
    using Crest.DataAccess;
    using FluentAssertions;
    using Xunit;

    public class QueryableExtensionsTests
    {
        public sealed class Apply : QueryableExtensionsTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => QueryableExtensions.Apply<string>(null);

                action.Should().Throw<ArgumentNullException>();
            }
        }
    }
}
