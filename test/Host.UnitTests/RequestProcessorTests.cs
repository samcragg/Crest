namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using NUnit.Framework;

    [TestFixture]
    public class RequestProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IContentConverterFactory converterFactory;
        private IRouteMapper mapper;
        private RequestProcessor processor;
        private IRequestData request;
        private IResponseStatusGenerator responseGenerator;
        private IServiceLocator serviceLocator;

        [SetUp]
        public void SetUp()
        {
            IServiceRegister register = Substitute.For<IServiceRegister>();
            this.bootstrapper = Substitute.For<Bootstrapper>(register);

            this.converterFactory = Substitute.For<IContentConverterFactory>();
            this.mapper = Substitute.For<IRouteMapper>();
            this.request = Substitute.For<IRequestData>();
            this.responseGenerator = Substitute.For<IResponseStatusGenerator>();
            this.serviceLocator = register;

            this.request.Handler.Returns(Substitute.For<MethodInfo>());

            this.serviceLocator.GetContentConverterFactory().Returns(this.converterFactory);
            this.serviceLocator.GetResponseStatusGenerator().Returns(this.responseGenerator);

            this.bootstrapper.RouteMapper.Returns(this.mapper);

            // NOTE: We're using ForPartsOf - make sure that all setup calls
            //       in tests use an argument matcher to avoid calling the real
            //       code: http://nsubstitute.github.io/help/partial-subs/
            this.processor = Substitute.ForPartsOf<RequestProcessor>(this.bootstrapper);
        }

        [TestFixture]
        public sealed class Constructor : RequestProcessorTests
        {
            [Test]
            public void ShouldCheckForNullArguments()
            {
                new Action(() => new FakeRequestProcessor(null))
                    .ShouldThrow<ArgumentNullException>();
            }

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

        [TestFixture]
        public sealed class HandleRequestAsync : RequestProcessorTests
        {
            private static readonly MethodInfo FakeMethodInfo =
                typeof(HandleRequestAsync).GetMethod(nameof(FakeMethod));

            private readonly RequestProcessor.MatchResult simpleMatch =
                new RequestProcessor.MatchResult(FakeMethodInfo, new Dictionary<string, object>());

            [Test]
            public async Task ShouldProvideANonInvokableMethodToOverrideMatches()
            {
                var capturedMatch = default(RequestProcessor.MatchResult);
                var callback = new Func<RequestProcessor.MatchResult, IRequestData>(match =>
                {
                    capturedMatch = match;
                    return null;
                });

                await this.processor.HandleRequestAsync(
                    new RequestProcessor.MatchResult((r, c) => Task.FromResult<IResponseData>(null)),
                    callback);

                capturedMatch.IsOverride.Should().BeFalse();
                capturedMatch.Method.Invoking(m => m.Invoke(null, null))
                             .ShouldThrow<TargetInvocationException>()
                             .WithInnerException<InvalidOperationException>();
            }

            [Test]
            public async Task ShouldReturnNotAcceptableIfNoConverter()
            {
                this.converterFactory.GetConverter(null)
                    .ReturnsForAnyArgs((IContentConverter)null);
                IRequestData request = Substitute.For<IRequestData>();

                await this.processor.HandleRequestAsync(this.simpleMatch, r => request);

                await this.responseGenerator.Received().NotAcceptableAsync(request);
            }

            [Test]
            public async Task ShouldInvokeTheOverrideMethod()
            {
                IRequestData capturedRequest = null;
                IContentConverter capturedConverter = null;
                IResponseData response = Substitute.For<IResponseData>();
                RequestProcessor.OverrideMethod overrideMethod = (r, c) =>
                {
                    capturedConverter = c;
                    capturedRequest = r;
                    return Task.FromResult(response);
                };

                IRequestData request = Substitute.For<IRequestData>();

                await this.processor.HandleRequestAsync(
                    new RequestProcessor.MatchResult(overrideMethod),
                    _ => request);

                capturedRequest.Should().BeSameAs(request);
                capturedConverter.Should().NotBeNull();
                await this.processor.Received().WriteResponseAsync(request, response);
            }

            [Test]
            public async Task ShouldInvokeThePipelineInTheCorrectOrder()
            {
                this.processor.WhenForAnyArgs(p => p.OnAfterRequestAsync(null, null)).DoNotCallBase();
                this.processor.WhenForAnyArgs(p => p.OnBeforeRequestAsync(null)).DoNotCallBase();
                this.processor.WhenForAnyArgs(p => p.InvokeHandlerAsync(null, null)).DoNotCallBase();
                this.processor.OnBeforeRequestAsync(null).ReturnsForAnyArgs((IResponseData)null);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                Received.InOrder(() =>
                {
                    this.processor.OnBeforeRequestAsync(this.request);
                    this.processor.InvokeHandlerAsync(this.request, Arg.Is<IContentConverter>(c => c != null));
                    this.processor.OnAfterRequestAsync(this.request, Arg.Any<IResponseData>());
                });
            }

            [Test]
            public async Task ShouldReturnNonNullValuesFromOnBeforeRequest()
            {
                IResponseData response = Substitute.For<IResponseData>();
                this.processor.OnBeforeRequestAsync(Arg.Is(this.request)).Returns(response);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().WriteResponseAsync(this.request, response);
                await this.processor.DidNotReceiveWithAnyArgs().InvokeHandlerAsync(null, null);
            }

            [Test]
            public async Task ShouldCatchExceptionsFromInvokeHandler()
            {
                IResponseData response = Substitute.For<IResponseData>();
                Exception exception = new DivideByZeroException();
                this.processor.InvokeHandlerAsync(Arg.Is(this.request), Arg.Any<IContentConverter>()).Throws(exception);
                this.processor.OnErrorAsync(Arg.Is(this.request), exception).Returns(response);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().WriteResponseAsync(this.request, response);
            }

            public void FakeMethod()
            {
            }
        }

        [TestFixture]
        public sealed class InvokeHandlerAsync : RequestProcessorTests
        {
            private readonly IContentConverter converter = Substitute.For<IContentConverter>();

            [Test]
            public async Task ShouldReturnOKStatusCode()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>("Response"));

                IResponseData result = await this.processor.InvokeHandlerAsync(this.request, this.converter);

                result.StatusCode.Should().Be(200);
            }

            [Test]
            public void ShouldCheckTheHandlerIsFound()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns((RouteMethod)null);

                new Func<Task>(async () => await this.processor.InvokeHandlerAsync(this.request, this.converter))
                    .ShouldThrow<InvalidOperationException>();
            }

            [Test]
            public async Task ShouldSerializeTheResult()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>("Response"));

                using (var stream = new MemoryStream())
                {
                    IResponseData response = await this.processor.InvokeHandlerAsync(this.request, this.converter);
                    await response.WriteBody(stream);
                }

                this.converter.Received().WriteTo(Arg.Any<Stream>(), "Response");
            }

            [Test]
            public async Task ShouldInvokeNoContentStatusCodeHandler()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>(NoContent.Value));

                await this.processor.InvokeHandlerAsync(this.request, this.converter);

                await this.responseGenerator.ReceivedWithAnyArgs().NoContentAsync(null, null);
            }

            [Test]
            public async Task ShouldInvokeNotFoundStatusCodeHandler()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>(null));

                await this.processor.InvokeHandlerAsync(this.request, this.converter);

                await this.responseGenerator.ReceivedWithAnyArgs().NotFoundAsync(null, null);
            }
        }

        [TestFixture]
        public sealed class Match : RequestProcessorTests
        {
            [Test]
            public void ShouldReturnAnOverrideIfNoMethodMatches()
            {
                IReadOnlyDictionary<string, object> notUsed;
                this.mapper.Match(null, null, null, out notUsed).ReturnsForAnyArgs((MethodInfo)null);

                RequestProcessor.MatchResult result =
                    this.processor.Match("", "", Substitute.For<ILookup<string, string>>());

                result.IsOverride.Should().BeTrue();
                result.Override.Should().NotBeNull();
                result.Method.Should().BeNull();
                result.Parameters.Should().BeNull();
            }

            [Test]
            public void ShouldReturnTheMatchedInformation()
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

                result.IsOverride.Should().BeFalse();
                result.Method.Should().BeSameAs(method);
                result.Override.Should().BeNull();
                result.Parameters.Should().BeSameAs(parameters);
            }
        }

        [TestFixture]
        public sealed class OnAfterRequestAsync : RequestProcessorTests
        {
            [Test]
            public async Task ShouldInvokeThePluginsInTheCorrectOrder()
            {
                IPostRequestPlugin one = CreatePostRequestPlugin(1);
                IPostRequestPlugin two = CreatePostRequestPlugin(2);
                IPostRequestPlugin three = CreatePostRequestPlugin(3);
                this.serviceLocator.GetAfterRequestPlugins().Returns(new[] { three, one, two });

                await this.processor.OnAfterRequestAsync(this.request, Substitute.For<IResponseData>());

                Received.InOrder(() =>
                {
                    one.ProcessAsync(this.request, Arg.Any<IResponseData>());
                    two.ProcessAsync(this.request, Arg.Any<IResponseData>());
                    three.ProcessAsync(this.request, Arg.Any<IResponseData>());
                });
            }

            private static IPostRequestPlugin CreatePostRequestPlugin(int order)
            {
                IPostRequestPlugin plugin = Substitute.For<IPostRequestPlugin>();
                plugin.Order.Returns(order);
                plugin.ProcessAsync(null, null).ReturnsForAnyArgs((IResponseData)null);
                return plugin;
            }
        }

        [TestFixture]
        public sealed class OnBeforeRequestAsync : RequestProcessorTests
        {
            [Test]
            public async Task ShouldInvokeThePluginsInTheCorrectOrder()
            {
                IPreRequestPlugin one = CreatePreRequestPlugin(1);
                IPreRequestPlugin two = CreatePreRequestPlugin(2);
                IPreRequestPlugin three = CreatePreRequestPlugin(3);
                this.serviceLocator.GetBeforeRequestPlugins().Returns(new[] { three, one, two });

                await this.processor.OnBeforeRequestAsync(this.request);

                Received.InOrder(() =>
                {
                    one.ProcessAsync(this.request);
                    two.ProcessAsync(this.request);
                    three.ProcessAsync(this.request);
                });
            }

            [Test]
            public async Task ShouldReturnTheReturnedRepsonse()
            {
                IResponseData response = Substitute.For<IResponseData>();
                IPreRequestPlugin plugin = CreatePreRequestPlugin(1);
                plugin.ProcessAsync(this.request).Returns(response);
                this.serviceLocator.GetBeforeRequestPlugins().Returns(new[] { plugin });

                IResponseData result = await this.processor.OnBeforeRequestAsync(this.request);

                result.Should().BeSameAs(response);
            }

            private static IPreRequestPlugin CreatePreRequestPlugin(int order)
            {
                IPreRequestPlugin plugin = Substitute.For<IPreRequestPlugin>();
                plugin.Order.Returns(order);
                plugin.ProcessAsync(null).ReturnsForAnyArgs((IResponseData)null);
                return plugin;
            }
        }

        [TestFixture]
        public sealed class OnErrorAsync : RequestProcessorTests
        {
            [Test]
            public async Task ShouldInvokeThePluginsInTheCorrectOrder()
            {
                Exception exception = new DivideByZeroException();
                IErrorHandlerPlugin one = CreateErrorHandlerPlugin(1);
                IErrorHandlerPlugin two = CreateErrorHandlerPlugin(2);
                IErrorHandlerPlugin three = CreateErrorHandlerPlugin(3);
                this.serviceLocator.GetErrorHandlers().Returns(new[] { three, one, two });

                await this.processor.OnErrorAsync(this.request, exception);

                Received.InOrder(() =>
                {
                    one.CanHandle(exception);
                    two.CanHandle(exception);
                    three.CanHandle(exception);
                });
            }

            [Test]
            public async Task ShouldInvokeProcessIfCanHandleReturnsTrue()
            {
                Exception exception = new DivideByZeroException();
                IErrorHandlerPlugin plugin = CreateErrorHandlerPlugin(1);
                plugin.CanHandle(exception).Returns(true);
                this.serviceLocator.GetErrorHandlers().Returns(new[] { plugin });

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
        }
    }
}
