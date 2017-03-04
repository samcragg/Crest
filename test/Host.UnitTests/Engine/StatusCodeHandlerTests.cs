namespace Host.UnitTests.Engine
{
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class StatusCodeHandlerTests
    {
        private StatusCodeHandler handler;

        [SetUp]
        public void SetUp()
        {
            this.handler = new FakeStatusCodeHandler();
        }

        [TestFixture]
        public sealed class InternalErrorAsync : StatusCodeHandlerTests
        {
            [Test]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.InternalErrorAsync(null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        [TestFixture]
        public sealed class NoContentAsync : StatusCodeHandlerTests
        {
            [Test]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NoContentAsync(null, null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        [TestFixture]
        public sealed class NotAcceptableAsync : StatusCodeHandlerTests
        {
            [Test]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NotAcceptableAsync(null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        [TestFixture]
        public sealed class NotFoundAsync : StatusCodeHandlerTests
        {
            [Test]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NotFoundAsync(null, null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        private class FakeStatusCodeHandler : StatusCodeHandler
        {
            public override int Order
            {
                get { return 0; }
            }
        }
    }
}
