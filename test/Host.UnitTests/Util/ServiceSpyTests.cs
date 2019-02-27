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

            void OptionalMethod(string optional = null);
        }

        public sealed class InvokeAction : ServiceSpyTests
        {
            [Fact]
            public void ShouldReturnOptionalArgumentsThatWereNotSpecified()
            {
                (MethodInfo method, object[] args) = this.spy.InvokeAction(
                    (IFakeInterface x) => x.OptionalMethod());

                args.Should().ContainSingle().Which.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheMethodWithItsArguments()
            {
                (MethodInfo method, object[] args) = this.spy.InvokeAction(
                    (IFakeInterface x) => x.Method("a", 1));

                method.Name.Should().Be(nameof(IFakeInterface.Method));
                args.Should().HaveElementAt(0, "a");
                args.Should().HaveElementAt(1, 1);
            }

            [Fact]
            public void ShouldThrowIfNoMethodCalls()
            {
                Action action = () => this.spy.InvokeAction((IFakeInterface _) => { });

                action.Should().Throw<InvalidOperationException>();
            }
        }
    }
}
