namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RequestProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IContentConverterFactory converterFactory;
        private IRouteMapper mapper;
        private RequestProcessor processor;
        private IRequestData request;
        private ResponseGenerator responseGenerator;

        [SetUp]
        public void SetUp()
        {
            this.bootstrapper = Substitute.For<Bootstrapper>();
            this.converterFactory = Substitute.For<IContentConverterFactory>();
            this.mapper = Substitute.For<IRouteMapper>();
            this.request = Substitute.For<IRequestData>();
            this.responseGenerator = Substitute.For<ResponseGenerator>(Enumerable.Empty<StatusCodeHandler>());

            this.bootstrapper.GetService<IContentConverterFactory>().Returns(this.converterFactory);
            this.bootstrapper.GetService<IRouteMapper>().Returns(this.mapper);
            this.bootstrapper.GetService<ResponseGenerator>().Returns(this.responseGenerator);

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
        public async Task HandleRequestAsyncShouldCallTheMethodsInTheCorrectOrder()
        {
            this.processor.WhenForAnyArgs(p => p.OnAfterRequestAsync(null, null)).DoNotCallBase();
            this.processor.WhenForAnyArgs(p => p.OnBeforeRequestAsync(null)).DoNotCallBase();
            this.processor.WhenForAnyArgs(p => p.InvokeHandlerAsync(null)).DoNotCallBase();
            this.processor.OnBeforeRequestAsync(null).ReturnsForAnyArgs((IResponseData)null);

            await this.processor.HandleRequestAsync(this.request);

            Received.InOrder(() =>
            {
                this.processor.OnBeforeRequestAsync(this.request);
                this.processor.InvokeHandlerAsync(this.request);
                this.processor.OnAfterRequestAsync(this.request, Arg.Any<IResponseData>());
            });
        }

        [Test]
        public async Task HandleRequestAsyncShouldReturnNonNullValuesFromOnBeforeRequest()
        {
            IResponseData response = Substitute.For<IResponseData>();
            this.processor.OnBeforeRequestAsync(Arg.Is(this.request)).Returns(response);

            await this.processor.HandleRequestAsync(this.request);

            await this.processor.Received().WriteResponseAsync(this.request, response);
            await this.processor.DidNotReceive().InvokeHandlerAsync(Arg.Any<IRequestData>());
        }

        [Test]
        public async Task HandleRequestAsyncShouldCatchExceptionsFromInvokeHandler()
        {
            IResponseData response = Substitute.For<IResponseData>();
            Exception exception = new DivideByZeroException();
            this.processor.InvokeHandlerAsync(Arg.Is(this.request)).Throws(exception);
            this.processor.OnErrorAsync(Arg.Is(this.request), exception).Returns(response);

            await this.processor.HandleRequestAsync(this.request);

            await this.processor.Received().WriteResponseAsync(this.request, response);
        }

        [Test]
        public async Task InvokeHandlerAsyncShouldInvokeNoContentStatusCodeHandler()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns(_ => Task.FromResult<object>(NoContent.Value));

            await this.processor.InvokeHandlerAsync(this.request);

            await this.responseGenerator.ReceivedWithAnyArgs().NoContentAsync(null, null);
        }

        [Test]
        public async Task InvokeHandlerAsyncShouldReturnOKStatusCode()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns(_ => Task.FromResult<object>("Response"));

            IResponseData result = await this.processor.InvokeHandlerAsync(this.request);

            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void InvokeHandlerAsyncShouldCheckTheHandlerIsFound()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns((RouteMethod)null);

            Assert.That(
                async ()=> await this.processor.InvokeHandlerAsync(this.request),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public async Task InvokeHandlerAsyncShouldSerializeTheResult()
        {
            this.request.Handler.Returns(Substitute.For<MethodInfo>());
            this.mapper.GetAdapter(this.request.Handler)
                       .Returns(_ => Task.FromResult<object>("Response"));

            IResponseData result = await this.processor.InvokeHandlerAsync(this.request);

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
        public async Task OnAfterRequestAsyncShouldInvokeThePluginsInTheCorrectOrder()
        {
            IPostRequestPlugin one = CreatePostRequestPlugin(1);
            IPostRequestPlugin two = CreatePostRequestPlugin(2);
            IPostRequestPlugin three = CreatePostRequestPlugin(3);
            this.bootstrapper.GetAfterRequestPlugins().Returns(new[] { three, one, two });

            await this.processor.OnAfterRequestAsync(this.request, Substitute.For<IResponseData>());

            Received.InOrder(() =>
            {
                one.ProcessAsync(this.request, Arg.Any<IResponseData>());
                two.ProcessAsync(this.request, Arg.Any<IResponseData>());
                three.ProcessAsync(this.request, Arg.Any<IResponseData>());
            });
        }

        [Test]
        public async Task OnBeforeRequestAsyncShouldInvokeThePluginsInTheCorrectOrder()
        {
            IPreRequestPlugin one = CreatePreRequestPlugin(1);
            IPreRequestPlugin two = CreatePreRequestPlugin(2);
            IPreRequestPlugin three = CreatePreRequestPlugin(3);
            this.bootstrapper.GetBeforeRequestPlugins().Returns(new[] { three, one, two });

            await this.processor.OnBeforeRequestAsync(this.request);

            Received.InOrder(() =>
            {
                one.ProcessAsync(this.request);
                two.ProcessAsync(this.request);
                three.ProcessAsync(this.request);
            });
        }

        [Test]
        public async Task OnBeforeRequestAsyncShouldReturnTheReturnedRepsonse()
        {
            IResponseData response = Substitute.For<IResponseData>();
            IPreRequestPlugin plugin = CreatePreRequestPlugin(1);
            plugin.ProcessAsync(this.request).Returns(response);
            this.bootstrapper.GetBeforeRequestPlugins().Returns(new[] { plugin });

            IResponseData result = await this.processor.OnBeforeRequestAsync(this.request);

            Assert.That(result, Is.SameAs(response));
        }

        [Test]
        public async Task OnErrorAsyncShouldInvokeThePluginsInTheCorrectOrder()
        {
            Exception exception = new DivideByZeroException();
            IErrorHandlerPlugin one = CreateErrorHandlerPlugin(1);
            IErrorHandlerPlugin two = CreateErrorHandlerPlugin(2);
            IErrorHandlerPlugin three = CreateErrorHandlerPlugin(3);
            this.bootstrapper.GetErrorHandlers().Returns(new[] { three, one, two });

            await this.processor.OnErrorAsync(this.request, exception);

            Received.InOrder(() =>
            {
                one.CanHandle(exception);
                two.CanHandle(exception);
                three.CanHandle(exception);
            });
        }

        [Test]
        public async Task OnErrorAsyncShouldInvokeProcessIfCanHandleReturnsTrue()
        {
            Exception exception = new DivideByZeroException();
            IErrorHandlerPlugin plugin = CreateErrorHandlerPlugin(1);
            plugin.CanHandle(exception).Returns(true);
            this.bootstrapper.GetErrorHandlers().Returns(new[] { plugin });

            await this.processor.OnErrorAsync(this.request, exception);

            await plugin.Received().ProcessAsync(this.request, exception);
        }

        private static IErrorHandlerPlugin CreateErrorHandlerPlugin(int order)
        {
            IErrorHandlerPlugin plugin = Substitute.For<IErrorHandlerPlugin>();
            plugin.Order.Returns(order);
            plugin.CanHandle(null).ReturnsForAnyArgs(false);
            plugin.ProcessAsync(null, null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        private static IPostRequestPlugin CreatePostRequestPlugin(int order)
        {
            IPostRequestPlugin plugin = Substitute.For<IPostRequestPlugin>();
            plugin.Order.Returns(order);
            plugin.ProcessAsync(null, null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        private static IPreRequestPlugin CreatePreRequestPlugin(int order)
        {
            IPreRequestPlugin plugin = Substitute.For<IPreRequestPlugin>();
            plugin.Order.Returns(order);
            plugin.ProcessAsync(null).ReturnsForAnyArgs((IResponseData)null);
            return plugin;
        }

        // Used to test the constructor
        private class FakeRequestProcessor : RequestProcessor
        {
            public FakeRequestProcessor(Bootstrapper bootstrapper) : base(bootstrapper)
            {
            }

            protected internal override Task WriteResponseAsync(IRequestData request, IResponseData response)
            {
                throw new NotImplementedException();
            }
        }
    }
}
