namespace DataAccess.UnitTests.Expressions
{
    using System.Linq.Expressions;
    using Crest.DataAccess.Expressions;
    using FluentAssertions;
    using Xunit;

    public class ParameterVisitorTests
    {
        private readonly ParameterVisitor visitor = new ParameterVisitor();

        public sealed class FindParameter : ParameterVisitorTests
        {
            [Fact]
            public void ShouldReturnNullForMultipleParameters()
            {
                ParameterExpression parameter1 = Expression.Parameter(typeof(string));
                ParameterExpression parameter2 = Expression.Parameter(typeof(string));
                Expression expression = Expression.Assign(parameter1, parameter2);

                ParameterExpression result = this.visitor.FindParameter(expression, typeof(string));

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheParameterIsOfTheWrongType()
            {
                ParameterExpression parameter = Expression.Parameter(typeof(string));
                Expression expression = Expression.Assign(parameter, Expression.Constant(""));

                ParameterExpression result = this.visitor.FindParameter(expression, typeof(int));

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheSingleParameterOfTheSpecifiedType()
            {
                ParameterExpression parameter = Expression.Parameter(typeof(string));
                Expression expression = Expression.Assign(parameter, Expression.Constant(""));

                ParameterExpression result = this.visitor.FindParameter(expression, typeof(string));

                result.Should().BeSameAs(parameter);
            }
        }
    }
}
