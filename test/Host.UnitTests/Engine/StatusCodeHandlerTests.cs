namespace Host.UnitTests.Engine
{
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Xunit;

    public class StatusCodeHandlerTests
    {
        private readonly StatusCodeHandler handler = new FakeStatusCodeHandler();

        public sealed class InternalErrorAsync : StatusCodeHandlerTests
        {
            [Fact]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.InternalErrorAsync(null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        public sealed class NoContentAsync : StatusCodeHandlerTests
        {
            [Fact]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NoContentAsync(null, null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        public sealed class NotAcceptableAsync : StatusCodeHandlerTests
        {
            [Fact]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NotAcceptableAsync(null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        public sealed class NotFoundAsync : StatusCodeHandlerTests
        {
            [Fact]
            public void ShouldReturnACompletedTaskWithNull()
            {
                Task<IResponseData> response = this.handler.NotFoundAsync(null, null);

                response.IsCompleted.Should().BeTrue();
                response.Result.Should().BeNull();
            }
        }

        private class FakeStatusCodeHandler : StatusCodeHandler
        {
            public override int Order => 0;
        }
    }
}
