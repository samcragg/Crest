namespace Host.AspNetCore.UnitTests
{
    using Crest.Host;
    using Crest.Host.AspNetCore;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HttpContextProcessorTests
    {
        private Bootstrapper bootstrapper;
        private HttpContextProcessor processor;

        [SetUp]
        public void SetUp()
        {
            this.bootstrapper = Substitute.For<Bootstrapper>();
            this.processor = new HttpContextProcessor(this.bootstrapper);
        }
    }
}
