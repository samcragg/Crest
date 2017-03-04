namespace Host.UnitTests.Engine
{
    using System;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class ResponseGeneratorTests
    {
        private readonly Task<IResponseData> NullResponse = Task.FromResult<IResponseData>(null);
        private ResponseGenerator generator;
        private StatusCodeHandler handler;

        [SetUp]
        public void SetUp()
        {
            this.handler = Substitute.For<StatusCodeHandler>();
            this.generator = new ResponseGenerator(new[] { this.handler });
        }

        [Test]
        public async Task ShouldInvokeTheHandlersInOrder()
        {
            var handler1 = Substitute.For<StatusCodeHandler>();
            handler1.Order.Returns(1);
            handler1.NoContentAsync(null, null).ReturnsForAnyArgs(NullResponse);

            var handler2 = Substitute.For<StatusCodeHandler>();
            handler2.Order.Returns(2);
            handler2.NoContentAsync(null, null).ReturnsForAnyArgs(NullResponse);

            this.generator = new ResponseGenerator(new[] { handler2, handler1 });
            await this.generator.NoContentAsync(null, null);

            Received.InOrder(() =>
            {
                handler1.NoContentAsync(null, null);
                handler2.NoContentAsync(null, null);
            });
        }

        [TestFixture]
        public sealed class InternalErrorAsync : ResponseGeneratorTests
        {
            [Test]
            public async Task ShouldReturnTheHandlerResult()
            {
                Exception exception = new Exception();
                IResponseData response = Substitute.For<IResponseData>();
                this.handler.InternalErrorAsync(exception).Returns(response);

                IResponseData result = await this.generator.InternalErrorAsync(exception);

                result.Should().BeSameAs(response);
            }

            [Test]
            public async Task ShouldReturn500()
            {
                this.handler.InternalErrorAsync(null).ReturnsForAnyArgs(NullResponse);

                IResponseData response = await this.generator.InternalErrorAsync(null);

                response.StatusCode.Should().Be(500);
            }
        }

        [TestFixture]
        public sealed class NoContentAsync : ResponseGeneratorTests
        {
            [Test]
            public async Task ShouldReturnTheHandlerResult()
            {
                IRequestData request = Substitute.For<IRequestData>();
                IContentConverter converter = Substitute.For<IContentConverter>();
                IResponseData response = Substitute.For<IResponseData>();
                this.handler.NoContentAsync(request, converter).Returns(response);

                IResponseData result = await this.generator.NoContentAsync(request, converter);

                result.Should().BeSameAs(response);
            }

            [Test]
            public async Task ShouldReturn204()
            {
                this.handler.NoContentAsync(null, null).ReturnsForAnyArgs(NullResponse);

                IResponseData response = await this.generator.NoContentAsync(null, null);

                response.StatusCode.Should().Be(204);
            }
        }

        [TestFixture]
        public sealed class NotAcceptableAsync : ResponseGeneratorTests
        {
            [Test]
            public async Task ShouldReturnTheHandlerResult()
            {
                IRequestData request = Substitute.For<IRequestData>();
                IResponseData response = Substitute.For<IResponseData>();
                this.handler.NotAcceptableAsync(request).Returns(response);

                IResponseData result = await this.generator.NotAcceptableAsync(request);

                result.Should().BeSameAs(response);
            }

            [Test]
            public async Task ShouldReturn406()
            {
                this.handler.NotAcceptableAsync(null).ReturnsForAnyArgs(NullResponse);

                IResponseData response = await this.generator.NotAcceptableAsync(null);

                response.StatusCode.Should().Be(406);
            }
        }

        [TestFixture]
        public sealed class NotFoundAsync : ResponseGeneratorTests
        {
            [Test]
            public async Task ShouldReturnTheHandlerResult()
            {
                IRequestData request = Substitute.For<IRequestData>();
                IContentConverter converter = Substitute.For<IContentConverter>();
                IResponseData response = Substitute.For<IResponseData>();
                this.handler.NotFoundAsync(request, converter).Returns(response);

                IResponseData result = await this.generator.NotFoundAsync(request, converter);

                result.Should().BeSameAs(response);
            }

            [Test]
            public async Task ShouldReturn404()
            {
                this.handler.NotFoundAsync(null, null).ReturnsForAnyArgs(NullResponse);

                IResponseData response = await this.generator.NotFoundAsync(null, null);

                response.StatusCode.Should().Be(404);
            }
        }
    }
}
