namespace Host.UnitTests.Util.Internal
{
    using System;
    using System.Reflection;
    using Crest.Host.Util.Internal;
    using FluentAssertions;
    using Xunit;

    public class ServiceProxyTests
    {
        private readonly IFakeInterface fakeInterface;
        private readonly ServiceProxy proxy;

        public ServiceProxyTests()
        {
            this.fakeInterface = DispatchProxy.Create<IFakeInterface, ServiceProxy>();
            this.proxy = (ServiceProxy)this.fakeInterface;
        }

        public interface IFakeInterface
        {
            void Method(string arg);
        }

        public sealed class Arguments : ServiceProxyTests
        {
            [Fact]
            public void ShouldReturnTheArgumentsPassedToTheMethod()
            {
                this.fakeInterface.Method("value");

                object[] arguments = this.proxy.Arguments;

                arguments.Should().Equal(new object[] { "value" });
            }
        }

        public sealed class CalledMethod : ServiceProxyTests
        {
            [Fact]
            public void ShouldReturnTheInvokedMethod()
            {
                this.fakeInterface.Method("");

                MethodInfo method = this.proxy.CalledMethod;

                method.Name.Should().Be(nameof(IFakeInterface.Method));
            }
        }

        public sealed class Clear : ServiceProxyTests
        {
            [Fact]
            public void ShouldClearTheArguments()
            {
                this.fakeInterface.Method("");

                this.proxy.Clear();

                this.proxy.Arguments.Should().BeNull();
            }

            [Fact]
            public void ShouldClearTheCalledMethod()
            {
                this.fakeInterface.Method("");

                this.proxy.Clear();

                this.proxy.CalledMethod.Should().BeNull();
            }

            [Fact]
            public void ShouldNotAllowMultipleInvokations()
            {
                this.fakeInterface.Method("");

                this.fakeInterface.Invoking(i => i.Method(""))
                    .Should().Throw<InvalidOperationException>();
            }
        }
    }
}
