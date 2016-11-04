namespace Host.UnitTests
{
    using System;
    using Crest.Host;
    using Microsoft.AspNetCore.Hosting;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CrestWebHostBuilderExtensionsTests
    {
        [Test]
        public void UseCrestShouldCheckForNullArguments()
        {
            Assert.That(
                () => CrestWebHostBuilderExtensions.UseCrest(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void UseCrestShouldRegisterTheStartupClass()
        {
            IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

            CrestWebHostBuilderExtensions.UseCrest(builder);

            builder.Received().UseSetting(
                WebHostDefaults.ApplicationKey,
                Arg.Any<string>());
        }

        [Test]
        public void UseCrestShouldReturnThePassedInValue()
        {
            IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

            IWebHostBuilder result = CrestWebHostBuilderExtensions.UseCrest(builder);

            Assert.That(result, Is.SameAs(builder));
        }
    }
}
