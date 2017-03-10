namespace Host.AspNetCore.UnitTests
{
    using System;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Hosting;
    using NSubstitute;
    using Xunit;

    public class CrestWebHostBuilderExtensionsTests
    {
        public sealed class UseCrest : CrestWebHostBuilderExtensionsTests
        {
            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => CrestWebHostBuilderExtensions.UseCrest(null);

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ShouldRegisterTheStartupClass()
            {
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

                CrestWebHostBuilderExtensions.UseCrest(builder);

                builder.Received().UseSetting(
                    WebHostDefaults.ApplicationKey,
                    Arg.Any<string>());
            }

            [Fact]
            public void ShouldReturnThePassedInValue()
            {
                IWebHostBuilder builder = Substitute.For<IWebHostBuilder>();

                IWebHostBuilder result = CrestWebHostBuilderExtensions.UseCrest(builder);

                result.Should().BeSameAs(builder);
            }
        }
    }
}
