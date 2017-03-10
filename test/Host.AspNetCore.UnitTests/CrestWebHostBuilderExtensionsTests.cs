namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Hosting;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class CrestWebHostBuilderExtensionsTests
    {
        [TestFixture]
        public sealed class UseCrest : CrestWebHostBuilderExtensionsTests
        {
            [Test]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => CrestWebHostBuilderExtensions.UseCrest(null);

                action.ShouldThrow<ArgumentNullException>();
            }

            [Test]
            public void ShouldRegisterTheStartupClass()
            {
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

                CrestWebHostBuilderExtensions.UseCrest(builder);

                builder.Received().UseSetting(
                    WebHostDefaults.ApplicationKey,
                    Arg.Any<string>());
            }

            [Test]
            public void ShouldReturnThePassedInValue()
            {
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

                IWebHostBuilder result = CrestWebHostBuilderExtensions.UseCrest(builder);

                result.Should().BeSameAs(builder);
            }
        }
    }
}
