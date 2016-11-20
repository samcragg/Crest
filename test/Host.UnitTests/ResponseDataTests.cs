namespace Host.UnitTests
{
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Host;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ResponseDataTests
    {
        [Test]
        public void WriteBodyShouldNotBeNull()
        {
            var data = new ResponseData("", 0, body: null);

            Assert.That(data.WriteBody, Is.Not.Null);
        }

        [Test]
        public void WriteBodyShouldReturnACompletedTask()
        {
            var data = new ResponseData("", 0, body: null);
            var stream = Substitute.For<Stream>();

            Task write = data.WriteBody(stream);

            Assert.That(write, Is.Not.Null);
            Assert.That(write.IsCompleted, Is.True);
            Assert.That(stream.ReceivedCalls(), Is.Empty);
        }
    }
}
