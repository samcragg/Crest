namespace DataAccess.UnitTests
{
    using System;
    using System.Linq.Expressions;
    using Crest.DataAccess;
    using FluentAssertions;
    using Xunit;

    public class MappingInfoTests
    {
        public sealed class Constructor : MappingInfoTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Type type = typeof(object);
                Expression expression = Expression.Empty();

                new Action(() => new MappingInfo(null, type, expression))
                    .Should().Throw<ArgumentNullException>();

                new Action(() => new MappingInfo(type, null, expression))
                    .Should().Throw<ArgumentNullException>();

                new Action(() => new MappingInfo(type, type, null))
                    .Should().Throw<ArgumentNullException>();
            }
        }
    }
}
