namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteMethodAdapterTests
    {
        private readonly RouteMethodAdapter adapter = new RouteMethodAdapter();

        public interface IFakeInterface
        {
            Task<int> Int32Task();

            Task NonDefaultedOptions([Optional]int noDefault);

            int NonTaskMethod();

            Task OptionalParameter(int optional = 321);

            Task<object> SimpleMethod();

            Task SingleParameter(int value);

            Task TaskMethod();
        }

        public sealed class CreateMethod : RouteMethodAdapterTests
        {
            [Fact]
            public void ShouldCheckTheMethodReturnsATask()
            {
                MethodInfo nonTaskMethod = typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.NonTaskMethod));

                Action action = () => this.adapter.CreateMethod(() => null, nonTaskMethod);

                action.ShouldThrow<ArgumentException>();
            }

            [Fact]
            public void ShouldConstructTheObjectWithTheFactoryDelegate()
            {
                bool delegateCalled = false;
                FakeClass.ClearConstructorCount();

                RouteMethod result = this.adapter.CreateMethod(
                    () =>
                    {
                        delegateCalled = true;
                        return new FakeClass();
                    },
                    typeof(FakeClass).GetMethod(nameof(FakeClass.MyMethod)));

                delegateCalled.Should().BeFalse();
                FakeClass.ConstructorCount.Value.Should().Be(0);
                result(null);
                delegateCalled.Should().BeTrue();
                FakeClass.ConstructorCount.Value.Should().Be(1);
            }

            [Fact]
            public async Task ShouldConvertGenericTaskToTaskObject()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();
                instance.Int32Task().Returns(123);

                RouteMethod wrapper = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.Int32Task)));

                object result = await wrapper(null);

                result.Should().Be(123);
            }

            [Fact]
            public void ShouldInvokeTheSpecifiedMethod()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.SimpleMethod)));

                result(null);
                instance.Received().SimpleMethod();
            }

            [Fact]
            public void ShouldPassInDefaultedValuesForOptionalParametersWithDefaults()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.OptionalParameter)));

                result(new Dictionary<string, object>());

                instance.Received().OptionalParameter(321);
            }

            [Fact]
            public void ShouldPassInTheCapturedParameters()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.SingleParameter)));

                result(new Dictionary<string, object> { { "value", 1234 } });

                instance.Received().SingleParameter(1234);
            }

            [Fact]
            public void ShouldPassInTheCapturedValueForOptionalParameters()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.OptionalParameter)));

                result(new Dictionary<string, object> { { "optional", 123 } });

                instance.Received().OptionalParameter(123);
            }

            [Fact]
            public void ShouldPassInTheTypesDefaultValueForParametersMarkedAsOptional()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.NonDefaultedOptions)));

                result(new Dictionary<string, object>());

                instance.Received().NonDefaultedOptions(0);
            }

            [Fact]
            public async Task ShouldReturnNoContentValueForTaskMethods()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();

                RouteMethod wrapper = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.TaskMethod)));

                object result = await wrapper(null);

                result.Should().BeSameAs(NoContent.Value);
                await instance.Received().TaskMethod();
            }

            [Fact]
            public void ShouldThrowExceptionsInsideTheConvertedTask()
            {
                IFakeInterface instance = Substitute.For<IFakeInterface>();
                instance.TaskMethod().Returns(Task.FromException(new DivideByZeroException()));

                RouteMethod result = this.adapter.CreateMethod(
                    () => instance,
                    typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.TaskMethod)));

                Exception ex = result(null).Exception.Flatten();
                ex.InnerException.Should().BeOfType<DivideByZeroException>();
            }
        }

        private class FakeClass
        {
            internal static ThreadLocal<int> ConstructorCount = new ThreadLocal<int>();

            public FakeClass()
            {
                ConstructorCount.Value++;
            }

            public Task<object> MyMethod()
            {
                return Task.FromResult<object>(null);
            }

            internal static void ClearConstructorCount()
            {
                ConstructorCount.Value = 0;
            }
        }
    }
}
