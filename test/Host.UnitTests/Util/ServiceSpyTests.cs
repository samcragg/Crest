namespace Host.UnitTests.Util
{
    using System;
    using System.Reflection;
    using Crest.Host.Util;
    using FluentAssertions;
    using Xunit;

    public class ServiceSpyTests
    {
        private readonly ServiceSpy spy = new ServiceSpy();

        public interface IFakeInterface
        {
            string Method(string str, int integer);
        }

        public sealed class InvokeFunction : ServiceSpyTests
        {
            [Fact]
            public void ShouldReturnTheMethodWithItsArguments()
            {
                (MethodInfo method, object[] args) = this.spy.InvokeFunction(
                    (IFakeInterface x) => x.Method("a", 1));

                method.Name.Should().Be(nameof(IFakeInterface.Method));
                args.Should().HaveElementAt(0, "a");
                args.Should().HaveElementAt(1, 1);
            }

            [Fact]
            public void ShouldThrowIfNoMethodCalls()
            {
                Action action = () => this.spy.InvokeFunction((IFakeInterface _) => "");

                action.Should().Throw<InvalidOperationException>();
            }
        }
    }
}
