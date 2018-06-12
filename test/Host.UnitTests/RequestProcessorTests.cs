namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host;
    using Crest.Host.Engine;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using NSubstitute.ReturnsExtensions;
    using Xunit;

    public class RequestProcessorTests
    {
        private readonly Bootstrapper bootstrapper;
        private readonly IContentConverterFactory converterFactory;
        private readonly IRouteMapper mapper;
        private readonly RequestProcessor processor;
        private readonly IRequestData request;
        private readonly IResponseStatusGenerator responseGenerator;
        private readonly IServiceLocator serviceLocator;

        public RequestProcessorTests()
        {
            this.serviceLocator = Substitute.For<IServiceLocator>();
            this.bootstrapper = Substitute.For<Bootstrapper>(this.serviceLocator);

            this.converterFactory = Substitute.For<IContentConverterFactory>();
            this.mapper = Substitute.For<IRouteMapper>();
            this.request = Substitute.For<IRequestData>();
            this.responseGenerator = Substitute.For<IResponseStatusGenerator>();

            this.request.Handler.Returns(Substitute.For<MethodInfo>());

            this.serviceLocator.GetService(typeof(IContentConverterFactory))
                .Returns(this.converterFactory);

            this.serviceLocator.GetService(typeof(IResponseStatusGenerator))
                .Returns(this.responseGenerator);

            this.bootstrapper.RouteMapper.Returns(this.mapper);

            // NOTE: We're using ForPartsOf - make sure that all setup calls
            //       in tests use an argument matcher to avoid calling the real
            //       code: http://nsubstitute.github.io/help/partial-subs/
            this.processor = Substitute.ForPartsOf<RequestProcessor>(this.bootstrapper);
        }

        public sealed class Constructor : RequestProcessorTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => new FakeRequestProcessor(null);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldPrimeTheConvertersFactoryWithKnownReturnTypes()
            {
                const BindingFlags PrivateMethods = BindingFlags.Instance | BindingFlags.NonPublic;
                this.converterFactory.ClearReceivedCalls();
                this.mapper.GetKnownMethods().Returns(new[]
                {
                    typeof(Constructor).GetMethod(nameof(MethodReturningTask), PrivateMethods),
                    typeof(Constructor).GetMethod(nameof(MethodReturningTaskOfInt), PrivateMethods)
                });

                new FakeRequestProcessor(this.bootstrapper);

                this.converterFactory.Received().PrimeConverters(typeof(int));
            }

            private Task MethodReturningTask()
            {
                return null;
            }

            private Task<int> MethodReturningTaskOfInt()
            {
                return null;
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

        public sealed class HandleRequestAsync : RequestProcessorTests
        {
            private static readonly MethodInfo FakeMethodInfo =
                typeof(HandleRequestAsync).GetMethod(nameof(FakeMethod), BindingFlags.NonPublic | BindingFlags.Static);

            private readonly RequestProcessor.MatchResult simpleMatch =
                new RequestProcessor.MatchResult(FakeMethodInfo, new Dictionary<string, object>());

            [Fact]
            public async Task ShouldCatchExceptionsFromInvokeHandler()
            {
                IResponseData response = Substitute.For<IResponseData>();
                Exception exception = new DivideByZeroException();
                this.processor.InvokeHandlerAsync(Arg.Is(this.request), Arg.Any<IContentConverter>()).Throws(exception);
                this.processor.OnErrorAsync(Arg.Is(this.request), exception).Returns(response);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().WriteResponseAsync(this.request, response);
            }

            [Fact]
            public async Task ShouldCatchExceptionsFromTheErrorHandler()
            {
                Exception exception = new DivideByZeroException();
                this.processor.InvokeHandlerAsync(Arg.Is(this.request), Arg.Any<IContentConverter>())
                    .Throws<ArgumentNullException>();
                this.processor.OnErrorAsync(Arg.Is(this.request), Arg.Any<Exception>())
                    .Throws(exception);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.responseGenerator.Received().InternalErrorAsync(exception);
            }

            [Fact]
            public async Task ShouldCatchExceptionsFromTheInternalErrorHandler()
            {
                this.processor.InvokeHandlerAsync(Arg.Is(this.request), Arg.Any<IContentConverter>())
                    .Throws<DivideByZeroException>();
                this.processor.OnErrorAsync(Arg.Is(this.request), Arg.Any<Exception>())
                    .Throws<DivideByZeroException>();
                this.responseGenerator.InternalErrorAsync(Arg.Any<Exception>())
                    .Throws<DivideByZeroException>();

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().WriteResponseAsync(this.request, ResponseGenerator.InternalError);
            }

            [Fact]
            public async Task ShouldCreateANewServiceLocatorScope()
            {
                IServiceLocator child = Substitute.For<IServiceLocator>();
                this.serviceLocator.CreateScope().Returns(child);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().OnBeforeRequestAsync(child, this.request);
            }

            [Fact]
            public async Task ShouldDisposeTheServiceLocatorScope()
            {
                IServiceLocator child = Substitute.For<IServiceLocator, IDisposable>();
                this.serviceLocator.CreateScope().Returns(child);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                ((IDisposable)child).Received().Dispose();
            }

            [Fact]
            public async Task ShouldInvokeTheOverrideMethod()
            {
                IRequestData capturedRequest = null;
                IContentConverter capturedConverter = null;
                IResponseData response = Substitute.For<IResponseData>();
                OverrideMethod overrideMethod = (r, c) =>
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

            [Fact]
            public async Task ShouldInvokeThePipelineInTheCorrectOrder()
            {
                this.processor.WhenForAnyArgs(async p => await p.OnAfterRequestAsync(null, null, null)).DoNotCallBase();
                this.processor.WhenForAnyArgs(async p => await p.OnBeforeRequestAsync(null, null)).DoNotCallBase();
                this.processor.WhenForAnyArgs(async p => await p.InvokeHandlerAsync(null, null)).DoNotCallBase();
                this.processor.OnBeforeRequestAsync(null, null).ReturnsForAnyArgs((IResponseData)null);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                Received.InOrder(async () =>
                {
                    await this.processor.OnBeforeRequestAsync(Arg.Any<IServiceLocator>(), this.request);
                    await this.processor.InvokeHandlerAsync(this.request, Arg.Is<IContentConverter>(c => c != null));
                    await this.processor.OnAfterRequestAsync(Arg.Any<IServiceLocator>(), this.request, Arg.Any<IResponseData>());
                });
            }

            [Fact]
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
                             .Should().Throw<TargetInvocationException>()
                             .WithInnerException<InvalidOperationException>();
            }

            [Fact]
            public async Task ShouldReturnNonNullValuesFromOnBeforeRequest()
            {
                IResponseData response = Substitute.For<IResponseData>();
                this.processor.OnBeforeRequestAsync(Arg.Any<IServiceLocator>(), Arg.Is(this.request))
                    .Returns(response);

                await this.processor.HandleRequestAsync(this.simpleMatch, _ => this.request);

                await this.processor.Received().WriteResponseAsync(this.request, response);
                await this.processor.DidNotReceiveWithAnyArgs().InvokeHandlerAsync(null, null);
            }

            [Fact]
            public async Task ShouldReturnNotAcceptableIfNoConverter()
            {
                this.converterFactory.GetConverterForAccept(null).ReturnsNullForAnyArgs();
                IRequestData request = Substitute.For<IRequestData>();

                await this.processor.HandleRequestAsync(this.simpleMatch, r => request);

                await this.responseGenerator.Received().NotAcceptableAsync(request);
            }

            [Fact]
            public async Task ShouldUpdateTheServiceLocatorCapture()
            {
                var placeholder = new ServiceProviderPlaceholder();
                this.request.Parameters.TryGetValue(ServiceProviderPlaceholder.Key, out object _)
                    .Returns(ci =>
                    {
                        ci[1] = placeholder;
                        return true;
                    });

                await this.processor.HandleRequestAsync(this.simpleMatch, r => this.request);

                placeholder.Provider.Should().NotBeNull();
            }

            internal static void FakeMethod()
            {
            }
        }

        public sealed class InvokeHandlerAsync : RequestProcessorTests
        {
            private readonly IContentConverter converter = Substitute.For<IContentConverter>();

            [Fact]
            public void ShouldCheckTheHandlerIsFound()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns((RouteMethod)null);

                new Func<Task>(async () => await this.processor.InvokeHandlerAsync(this.request, this.converter))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public async Task ShouldInvokeNoContentStatusCodeHandler()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>(NoContent.Value));

                await this.processor.InvokeHandlerAsync(this.request, this.converter);

                await this.responseGenerator.ReceivedWithAnyArgs().NoContentAsync(null, null);
            }

            [Fact]
            public async Task ShouldInvokeNotAcceptableIfUnableToUpdateTheRequstBodyPlaceholder()
            {
                RequestBodyPlaceholder placeholder = Substitute.For<RequestBodyPlaceholder>(typeof(object));
                placeholder.UpdateRequestAsync(null, null, null).ReturnsForAnyArgs(false);
                this.request.Parameters.Returns(new Dictionary<string, object> { ["body"] = placeholder });

                await this.processor.InvokeHandlerAsync(this.request, null);

                await this.responseGenerator.ReceivedWithAnyArgs().NotAcceptableAsync(null);
            }

            [Fact]
            public async Task ShouldInvokeNotFoundStatusCodeHandler()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>(null));

                await this.processor.InvokeHandlerAsync(this.request, this.converter);

                await this.responseGenerator.ReceivedWithAnyArgs().NotFoundAsync(null, null);
            }

            [Fact]
            public async Task ShouldReturnOKStatusCode()
            {
                this.mapper.GetAdapter(this.request.Handler)
                           .Returns(_ => Task.FromResult<object>("Response"));

                IResponseData result = await this.processor.InvokeHandlerAsync(this.request, this.converter);

                result.StatusCode.Should().Be(200);
            }

            [Fact]
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

            [Fact]
            public async Task ShouldUpdateRequestBodyPlaceholders()
            {
                RequestBodyPlaceholder placeholder = Substitute.For<RequestBodyPlaceholder>(typeof(object));
                this.request.Parameters.Returns(new Dictionary<string, object> { ["body"] = placeholder });

                await this.processor.InvokeHandlerAsync(this.request, null);

                await placeholder.ReceivedWithAnyArgs().UpdateRequestAsync(null, null, null);
            }
        }

        public sealed class Match : RequestProcessorTests
        {
            [Fact]
            public void ShouldReturnAnOverrideIfNoMethodMatches()
            {
                this.mapper.FindOverride(null, null).ReturnsNullForAnyArgs();
                this.mapper.Match(null, null, null, out _).ReturnsForAnyArgs((MethodInfo)null);

                RequestProcessor.MatchResult result =
                    this.processor.Match("", "", Substitute.For<ILookup<string, string>>());

                result.IsOverride.Should().BeTrue();
                result.Override.Should().NotBeNull();
                result.Method.Should().BeNull();
                result.Parameters.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnDirectRoutes()
            {
                OverrideMethod method = (r, c) => Task.FromResult<IResponseData>(null);
                this.mapper.FindOverride("GET", "/route").Returns(method);

                RequestProcessor.MatchResult result =
                    this.processor.Match("GET", "/route", Substitute.For<ILookup<string, string>>());

                result.IsOverride.Should().BeTrue();
                result.Override.Should().BeSameAs(method);
                this.mapper.DidNotReceiveWithAnyArgs().Match(null, null, null, out _);
            }

            [Fact]
            public void ShouldReturnTheMatchedInformation()
            {
                MethodInfo method = Substitute.For<MethodInfo>();
                IReadOnlyDictionary<string, object> parameters = new Dictionary<string, object>();
                ILookup<string, string> query = Substitute.For<ILookup<string, string>>();

                this.mapper.FindOverride("GET", "/route").ReturnsNull();

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

        public sealed class OnAfterRequestAsync : RequestProcessorTests
        {
            [Fact]
            public async Task ShouldInvokeThePluginsInTheCorrectOrder()
            {
                IPostRequestPlugin one = CreatePostRequestPlugin(1);
                IPostRequestPlugin two = CreatePostRequestPlugin(2);
                IPostRequestPlugin three = CreatePostRequestPlugin(3);
                this.serviceLocator.GetAfterRequestPlugins().Returns(new[] { three, one, two });

                await this.processor.OnAfterRequestAsync(this.serviceLocator, this.request, Substitute.For<IResponseData>());

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

        public sealed class OnBeforeRequestAsync : RequestProcessorTests
        {
            [Fact]
            public async Task ShouldInvokeThePluginsInTheCorrectOrder()
            {
                IPreRequestPlugin one = CreatePreRequestPlugin(1);
                IPreRequestPlugin two = CreatePreRequestPlugin(2);
                IPreRequestPlugin three = CreatePreRequestPlugin(3);
                this.serviceLocator.GetBeforeRequestPlugins().Returns(new[] { three, one, two });

                await this.processor.OnBeforeRequestAsync(this.serviceLocator, this.request);

                Received.InOrder(() =>
                {
                    one.ProcessAsync(this.request);
                    two.ProcessAsync(this.request);
                    three.ProcessAsync(this.request);
                });
            }

            [Fact]
            public async Task ShouldReturnTheReturnedRepsonse()
            {
                IResponseData response = Substitute.For<IResponseData>();
                IPreRequestPlugin plugin = CreatePreRequestPlugin(1);
                plugin.ProcessAsync(this.request).Returns(response);
                this.serviceLocator.GetBeforeRequestPlugins().Returns(new[] { plugin });

                IResponseData result = await this.processor.OnBeforeRequestAsync(this.serviceLocator, this.request);

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

        public sealed class OnErrorAsync : RequestProcessorTests
        {
            [Fact]
            public async Task ShouldInvokeProcessIfCanHandleReturnsTrue()
            {
                Exception exception = new DivideByZeroException();
                IErrorHandlerPlugin plugin = CreateErrorHandlerPlugin(1);
                plugin.CanHandle(exception).Returns(true);
                this.serviceLocator.GetErrorHandlers().Returns(new[] { plugin });

                await this.processor.OnErrorAsync(this.request, exception);

                await plugin.Received().ProcessAsync(this.request, exception);
            }

            [Fact]
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
