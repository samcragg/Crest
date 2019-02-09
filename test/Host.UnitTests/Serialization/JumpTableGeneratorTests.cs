namespace Host.UnitTests.Serialization
{
    using System.Linq.Expressions;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class JumpTableGeneratorTests
    {
        private readonly ParameterExpression executedExpression =
            Expression.Parameter(typeof(string).MakeByRefType());

        private readonly JumpTableGenerator generator = new JumpTableGenerator(new Methods());

        private delegate void ReferenceString(string value, ref string output);

        private void AddMapping(string key, string value)
        {
            this.generator.Add(
                key,
                Expression.Assign(this.executedExpression, Expression.Constant(value)));
        }

        private string InvokeSwitch(string value)
        {
            ParameterExpression input = Expression.Parameter(typeof(string));
            var lambda = Expression.Lambda<ReferenceString>(
                this.generator.Build(input),
                input,
                this.executedExpression);

            string output = null;
            lambda.Compile()(value, ref output);
            return output;
        }

        public sealed class Build : JumpTableGeneratorTests
        {
            [Fact]
            public void ShouldEmitASwitchTableForLotsOfConditions()
            {
                this.AddMapping("1", "one");
                this.AddMapping("2", "two");
                this.AddMapping("3", "three");
                this.AddMapping("4", "four");
                this.AddMapping("5", "five");
                this.AddMapping("6", "six");

                string result = this.InvokeSwitch("5");

                result.Should().Be("five");
            }

            [Fact]
            public void ShouldEmitMulitpleConditions()
            {
                this.AddMapping("1", "one");
                this.AddMapping("2", "two");

                string result = this.InvokeSwitch("2");

                result.Should().Be("two");
            }
        }
    }
}
