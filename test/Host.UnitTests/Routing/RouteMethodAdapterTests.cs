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
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RouteMethodAdapterTests
    {
        private RouteMethodAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            this.adapter = new RouteMethodAdapter();
        }

        [Test]
        public void ShouldCheckTheMethodReturnsATask()
        {
            MethodInfo nonTaskMethod = typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.NonTaskMethod));

            Assert.That(
                () => this.adapter.CreateMethod(() => null, nonTaskMethod),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
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

            Assert.That(delegateCalled, Is.False);
            Assert.That(FakeClass.ConstructorCount.Value, Is.EqualTo(0));
            result(null);
            Assert.That(delegateCalled, Is.True);
            Assert.That(FakeClass.ConstructorCount.Value, Is.EqualTo(1));
        }

        [Test]
        public void ShouldInvokeTheSpecifiedMethod()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.SimpleMethod)));

            result(null);
            instance.Received().SimpleMethod();
        }

        [Test]
        public void ShouldPassInTheCapturedParameters()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.SingleParameter)));

            result(new Dictionary<string, object> { { "value", 1234 } });

            instance.Received().SingleParameter(1234);
        }

        [Test]
        public void ShouldPassInTheCapturedValueForOptionalParameters()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.OptionalParameter)));

            result(new Dictionary<string, object> { { "optional", 123 } });

            instance.Received().OptionalParameter(123);
        }

        [Test]
        public void ShouldPassInDefaultedValuesForOptionalParametersWithDefaults()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.OptionalParameter)));

            result(new Dictionary<string, object>());

            instance.Received().OptionalParameter(321);
        }

        [Test]
        public void ShouldPassInTheTypesDefaultValueForParametersMarkedAsOptional()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.NonDefaultedOptions)));

            result(new Dictionary<string, object>());

            instance.Received().NonDefaultedOptions(0);
        }

        [Test]
        public async Task ShouldReturnNoContentValueForTaskMethods()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();

            RouteMethod wrapper = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.TaskMethod)));

            object result = await wrapper(null);

            Assert.That(result, Is.SameAs(NoContent.Value));
            await instance.Received().TaskMethod();
        }

        [Test]
        public async Task ShouldConvertGenericTaskToTaskObject()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();
            instance.Int32Task().Returns(123);

            RouteMethod wrapper = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.Int32Task)));

            object result = await wrapper(null);

            Assert.That(result, Is.EqualTo(123));
        }

        [Test]
        public void ShouldThrowExceptionsInsideTheConvertedTask()
        {
            IFakeInterface instance = Substitute.For<IFakeInterface>();
            instance.TaskMethod().Returns(Task.FromException(new DivideByZeroException()));

            RouteMethod result = this.adapter.CreateMethod(
                () => instance,
                typeof(IFakeInterface).GetMethod(nameof(IFakeInterface.TaskMethod)));

            Exception ex = result(null).Exception.Flatten();
            Assert.That(ex.InnerException, Is.InstanceOf<DivideByZeroException>());
        }

        public Task<object> MyReturn()
        {
            IFakeInterface instance = null;
            return instance.Int32Task().ContinueWith<object>(t => t.Result);
        }

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

        private class FakeClass
        {
            internal static ThreadLocal<int> ConstructorCount = new ThreadLocal<int>();

            public FakeClass()
            {
                ConstructorCount.Value++;
            }

            internal static void ClearConstructorCount()
            {
                ConstructorCount.Value = 0;
            }

            public Task<object> MyMethod()
            {
                return Task.FromResult<object>(null);
            }
        }
    }
}
