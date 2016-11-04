namespace Host.UnitTests
{
    using System;
    using Crest.Host;
    using Microsoft.AspNetCore.Hosting;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class WebHostBuilderExtensionsTests
    {
        [Test]
        public void UseCrestShouldCheckForNullArguments()
        {
            Assert.That(
                () => WebHostBuilderExtensions.UseCrest(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void UseCrestShouldReturnThePassedInValue()
        {
            IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

            IWebHostBuilder result = WebHostBuilderExtensions.UseCrest(builder);

            Assert.That(result, Is.SameAs(builder));
        }
    }
}
