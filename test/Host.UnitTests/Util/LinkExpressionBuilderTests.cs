namespace Host.UnitTests.Util
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host.Util;
    using FluentAssertions;
    using NSubstitute;
    using NSubstitute.Core;
    using Xunit;

    public class LinkExpressionBuilderTests
    {
        private readonly LinkExpressionBuilder builder = new LinkExpressionBuilder();

        public interface IInvalidMethods
        {
            [Get("/{missing_brace"), Version(1)]
            Task InvalidSyntax();

            [Version(1)]
            Task NoRoute();

            [Get("simple")]
            Task NoVersion();

            [Get("/{one}"), Version(1)]
            Task ParameterNotCaptured(int one, int two);
        }

        public interface IValidMethods
        {
            [Put("/body"), Version(1)]
            Task BodyParameter([FromBody]string body);

            [Get("dynamic{?anything*}"), Version(1)]
            Task DynamicQuery(dynamic anything);

            [Get("none"), Version(2)]
            Task NoParametersV2();

            [Get("query/{id}{?one,two}"), Version(1)]
            Task QueryParameter(string one, int id, string two = null);

            [Get("one/{id}"), Version(1)]
            Task SingleParameter(int id);
        }

        public sealed class FromMethod : LinkExpressionBuilderTests
        {
            [Fact]
            public void ShouldCallTheMinimumVersion()
            {
                string result = this.GetRoute<IValidMethods>(x => x.NoParametersV2());

                result.Should().Be("/v2/none");
            }

            [Fact]
            public void ShouldCallWithDynamicQueryValues()
            {
                string result = this.GetRoute<IValidMethods>(x => x.DynamicQuery(new { a = "test", b = 123 }));

                result.Should().Be("/v1/dynamic?a=test&b=123");
            }

            [Fact]
            public void ShouldCallWithParameterValues()
            {
                string result = this.GetRoute<IValidMethods>(x => x.SingleParameter(123));

                result.Should().Be("/v1/one/123");
            }

            [Fact]
            public void ShouldCallWithQueryValues()
            {
                string result = this.GetRoute<IValidMethods>(x => x.QueryParameter("1", 123, "2"));

                result.Should().Be("/v1/query/123?one=1&two=2");
            }

            [Fact]
            public void ShouldCallWithQueryValuesThatAreNotSpecified()
            {
                string result = this.GetRoute<IValidMethods>(x => x.QueryParameter("1", 123));

                result.Should().Be("/v1/query/123?one=1");
            }

            [Fact]
            public void ShouldIgnoreFromBodyParameters()
            {
                string result = this.GetRoute<IValidMethods>(x => x.BodyParameter("test"));

                result.Should().Be("/v1/body");
            }

            [Theory]
            [InlineData(nameof(IInvalidMethods.InvalidSyntax))]
            [InlineData(nameof(IInvalidMethods.NoRoute))]
            [InlineData(nameof(IInvalidMethods.NoVersion))]
            [InlineData(nameof(IInvalidMethods.ParameterNotCaptured))]
            public void ShouldThrowForInvalidMethods(string methodName)
            {
                MethodInfo method = typeof(IInvalidMethods).GetMethod(methodName);

                this.builder.Invoking(x => x.FromMethod<Func<object[], string>>(method))
                    .Should().Throw<InvalidOperationException>();
            }

            private string GetRoute<T>(Action<T> methodCall)
                where T : class
            {
                T instance = Substitute.For<T>();
                methodCall(instance);
                ICall call = instance.ReceivedCalls().Single();

                Func<object[], string> routeMethod =
                    this.builder.FromMethod<Func<object[], string>>(call.GetMethodInfo());
                return routeMethod(call.GetArguments());
            }
        }
    }
}
