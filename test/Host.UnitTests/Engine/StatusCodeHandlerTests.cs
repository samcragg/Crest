namespace Host.UnitTests.Engine
{
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
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

                Assert.That(response.IsCompleted, Is.True);
                Assert.That(response.Result, Is.Null);
            }
        }

        [Test]
        public void NoContentAsyncShouldReturnACompletedTaskWithNull()
        {
            Task<IResponseData> response = this.handler.NoContentAsync(null, null);

            Assert.That(response.IsCompleted, Is.True);
            Assert.That(response.Result, Is.Null);
        }

        [Test]
        public void NotAcceptableAsyncShouldReturnACompletedTaskWithNull()
        {
            Task<IResponseData> response = this.handler.NotAcceptableAsync(null);

            Assert.That(response.IsCompleted, Is.True);
            Assert.That(response.Result, Is.Null);
        }

        [Test]
        public void NotFoundAsyncShouldReturnACompletedTaskWithNull()
        {
            Task<IResponseData> response = this.handler.NotFoundAsync(null, null);

            Assert.That(response.IsCompleted, Is.True);
            Assert.That(response.Result, Is.Null);
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
