namespace Host.UnitTests.Engine
{
    using System;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;
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

                Assert.That(result, Is.SameAs(response));
            }

            [Test]
            public async Task ShouldReturn500()
            {
                this.handler.InternalErrorAsync(null).ReturnsForAnyArgs(NullResponse);

                IResponseData response = await this.generator.InternalErrorAsync(null);

                Assert.That(response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public async Task NoContentAsyncShouldReturnTheHandlerResult()
        {
            IRequestData request = Substitute.For<IRequestData>();
            IContentConverter converter = Substitute.For<IContentConverter>();
            IResponseData response = Substitute.For<IResponseData>();
            this.handler.NoContentAsync(request, converter).Returns(response);

            IResponseData result = await this.generator.NoContentAsync(request, converter);

            Assert.That(result, Is.SameAs(response));
        }

        [Test]
        public async Task NoContentAsyncShouldReturn204()
        {
            this.handler.NoContentAsync(null, null).ReturnsForAnyArgs(NullResponse);

            IResponseData response = await this.generator.NoContentAsync(null, null);

            Assert.That(response.StatusCode, Is.EqualTo(204));
        }

        [Test]
        public async Task NotAcceptableAsyncShouldReturnTheHandlerResult()
        {
            IRequestData request = Substitute.For<IRequestData>();
            IResponseData response = Substitute.For<IResponseData>();
            this.handler.NotAcceptableAsync(request).Returns(response);

            IResponseData result = await this.generator.NotAcceptableAsync(request);

            Assert.That(result, Is.SameAs(response));
        }

        [Test]
        public async Task NotAcceptableAsyncShouldReturn406()
        {
            this.handler.NotAcceptableAsync(null).ReturnsForAnyArgs(NullResponse);

            IResponseData response = await this.generator.NotAcceptableAsync(null);

            Assert.That(response.StatusCode, Is.EqualTo(406));
        }

        [Test]
        public async Task NotFoundAsyncShouldReturnTheHandlerResult()
        {
            IRequestData request = Substitute.For<IRequestData>();
            IContentConverter converter = Substitute.For<IContentConverter>();
            IResponseData response = Substitute.For<IResponseData>();
            this.handler.NotFoundAsync(request, converter).Returns(response);

            IResponseData result = await this.generator.NotFoundAsync(request, converter);

            Assert.That(result, Is.SameAs(response));
        }

        [Test]
        public async Task NotFoundAsyncShouldReturn404()
        {
            this.handler.NotFoundAsync(null, null).ReturnsForAnyArgs(NullResponse);

            IResponseData response = await this.generator.NotFoundAsync(null, null);

            Assert.That(response.StatusCode, Is.EqualTo(404));
        }
    }
}
