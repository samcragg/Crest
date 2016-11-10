namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RequestProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IRouteMapper mapper;
        private RequestProcessor processor;
        private IRequestData request;

        [SetUp]
        public void SetUp()
        {
            this.bootstrapper = Substitute.For<Bootstrapper>();
            this.mapper = Substitute.For<IRouteMapper>();
            this.request = Substitute.For<IRequestData>();

            this.bootstrapper.GetService<IRouteMapper>().Returns(this.mapper);

            // NOTE: We're using ForPartsOf - make sure that all setup calls
            //       in tests use an argument matcher to avoid calling the real
            //       code: http://nsubstitute.github.io/help/partial-subs/
            this.processor = Substitute.ForPartsOf<RequestProcessor>(this.bootstrapper);
        }

        [Test]
        public void ConstructorShouldCheckForNullArguments()
        {
            Assert.That(
                () => new FakeRequestProcessor(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public async Task HandleRequestShouldCallTheMethodsInTheCorrectOrder()
        {
            this.processor.WhenForAnyArgs(p => p.OnAfterRequest(null, null)).DoNotCallBase();
            this.processor.WhenForAnyArgs(p => p.OnBeforeRequest(null)).DoNotCallBase();
            this.processor.WhenForAnyArgs(p => p.InvokeHandler(null)).DoNotCallBase();
            this.processor.OnBeforeRequest(null).ReturnsForAnyArgs((IResponseData)null);

            await this.processor.HandleRequest(this.request);

            Received.InOrder(() =>
            {
                this.processor.OnBeforeRequest(this.request);
                this.processor.InvokeHandler(this.request);
                this.processor.OnAfterRequest(this.request, Arg.Any<IResponseData>());
            });
        }

        [Test]
        public async Task HandleRequestShouldReturnNonNullValuesFromOnBeforeRequest()
        {
            IResponseData response = Substitute.For<IResponseData>();
            this.processor.OnBeforeRequest(Arg.Is(this.request)).Returns(response);

            await this.processor.HandleRequest(this.request);

            await this.processor.Received().WriteResponse(this.request, response);
            await this.processor.DidNotReceive().InvokeHandler(Arg.Any<IRequestData>());
        }

        [Test]
        public async Task HandleRequestShouldCatchExceptionsFromInvokeHandler()
        {
            IResponseData response = Substitute.For<IResponseData>();
            Exception exception = new DivideByZeroException();
            this.processor.InvokeHandler(Arg.Is(this.request)).Throws(exception);
            this.processor.OnError(Arg.Is(this.request), exception).Returns(response);

            await this.processor.HandleRequest(this.request);

            await this.processor.Received().WriteResponse(this.request, response);
        }

        [Test]
        public async Task InvokeHandlerShouldReturnNoContentStatusCode()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns(_ => Task.FromResult<object>(NoContent.Value));

            IResponseData result = await this.processor.InvokeHandler(this.request);

            Assert.That(result.StatusCode, Is.EqualTo(204));
        }

        [Test]
        public async Task InvokeHandlerShouldReturnOKStatusCode()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns(_ => Task.FromResult<object>("Response"));

            IResponseData result = await this.processor.InvokeHandler(this.request);

            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void InvokeHandlerShouldCheckTheHandlerIsFound()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns((RouteMethod)null);

            Assert.That(
                async ()=> await this.processor.InvokeHandler(this.request),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void MatchShouldReturnFalseIfNotMethodMatches()
        {
            IReadOnlyDictionary<string, object> notUsed;
            this.mapper.Match(null, null, null, out notUsed).ReturnsForAnyArgs((MethodInfo)null);

            RequestProcessor.MatchResult result =
                this.processor.Match("", "", Substitute.For<ILookup<string, string>>());

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void MatchShouldReturnTheMatchedInformation()
        {
            MethodInfo method = Substitute.For<MethodInfo>();
            IReadOnlyDictionary<string, object> parameters = new Dictionary<string, object>();
            ILookup<string, string> query = Substitute.For<ILookup<string, string>>();

            // We need to call the Arg.XXX calls in the same order as the method
            // for NSubstitute to handle them and we need to use the specifier
            // for both string parameters so it doesn't get confused
            string verb = Arg.Is("GET");
            string path = Arg.Is("/route");
            IReadOnlyDictionary<string, object> any = Arg.Any<IReadOnlyDictionary<string, object>>();
            this.mapper.Match(verb, path, query, out any)
                .Returns(args =>
                {
                    args[3] = parameters;
                    return method;
                });

            RequestProcessor.MatchResult result =
                this.processor.Match("GET", "/route", query);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Method, Is.SameAs(method));
            Assert.That(result.Parameters, Is.SameAs(parameters));
        }

        [Test]
        public async Task OnAfterRequestShouldInvokeThePluginsInTheCorrectOrder()
        {
            IPostRequestPlugin one = CreatePostRequestPlugin(1);
            IPostRequestPlugin two = CreatePostRequestPlugin(2);
            IPostRequestPlugin three = CreatePostRequestPlugin(3);
            this.bootstrapper.GetAfterRequestPlugins().Returns(new[] { three, one, two });

            await this.processor.OnAfterRequest(this.request, Substitute.For<IResponseData>());

            Received.InOrder(() =>
            {
                one.Process(this.request, Arg.Any<IResponseData>());
                two.Process(this.request, Arg.Any<IResponseData>());
                three.Process(this.request, Arg.Any<IResponseData>());
            });
        }

        [Test]
        public async Task OnBeforeRequestShouldInvokeThePluginsInTheCorrectOrder()
        {
            IPreRequestPlugin one = CreatePreRequestPlugin(1);
            IPreRequestPlugin two = CreatePreRequestPlugin(2);
            IPreRequestPlugin three = CreatePreRequestPlugin(3);
            this.bootstrapper.GetBeforeRequestPlugins().Returns(new[] { three, one, two });

            await this.processor.OnBeforeRequest(this.request);

            Received.InOrder(() =>
            {
                one.Process(this.request);
                two.Process(this.request);
                three.Process(this.request);
            });
        }

        [Test]
        public async Task OnBeforeRequestShouldReturnTheReturnedRepsonse()
        {
            IResponseData response = Substitute.For<IResponseData>();
            IPreRequestPlugin plugin = CreatePreRequestPlugin(1);
            plugin.Process(this.request).Returns(response);
            this.bootstrapper.GetBeforeRequestPlugins().Returns(new[] { plugin });

            IResponseData result = await this.processor.OnBeforeRequest(this.request);

            Assert.That(result, Is.SameAs(response));
        }

        [Test]
        public async Task OnErrorShouldInvokeThePluginsInTheCorrectOrder()
        {
            Exception exception = new DivideByZeroException();
            IErrorHandlerPlugin one = CreateErrorHandlerPlugin(1);
            IErrorHandlerPlugin two = CreateErrorHandlerPlugin(2);
            IErrorHandlerPlugin three = CreateErrorHandlerPlugin(3);
            this.bootstrapper.GetErrorHandlers().Returns(new[] { three, one, two });

            await this.processor.OnError(this.request, exception);

            Received.InOrder(() =>
            {
                one.CanHandle(exception);
                two.CanHandle(exception);
                three.CanHandle(exception);
            });
        }

        [Test]
        public async Task OnErrorShouldInvokeProcessIfCanHandleReturnsTrue()
        {
            Exception exception = new DivideByZeroException();
            IErrorHandlerPlugin plugin = CreateErrorHandlerPlugin(1);
            plugin.CanHandle(exception).Returns(true);
            this.bootstrapper.GetErrorHandlers().Returns(new[] { plugin });

            await this.processor.OnError(this.request, exception);

            await plugin.Received().Process(this.request, exception);
        }

        private static IErrorHandlerPlugin CreateErrorHandlerPlugin(int order)
        {
            IErrorHandlerPlugin plugin = Substitute.For<IErrorHandlerPlugin>();
            plugin.Order.Returns(order);
            plugin.CanHandle(null).ReturnsForAnyArgs(false);
            plugin.Process(null, null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        private static IPostRequestPlugin CreatePostRequestPlugin(int order)
        {
            IPostRequestPlugin plugin = Substitute.For<IPostRequestPlugin>();
            plugin.Order.Returns(order);
            plugin.Process(null, null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        private static IPreRequestPlugin CreatePreRequestPlugin(int order)
        {
            IPreRequestPlugin plugin = Substitute.For<IPreRequestPlugin>();
            plugin.Order.Returns(order);
            plugin.Process(null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        // Used to test the constructor
        private class FakeRequestProcessor : RequestProcessor
        {
            public FakeRequestProcessor(Bootstrapper bootstrapper) : base(bootstrapper)
            {
            }

            protected internal override Task WriteResponse(IRequestData request, IResponseData response)
            {
                throw new NotImplementedException();
            }
        }
    }
}
