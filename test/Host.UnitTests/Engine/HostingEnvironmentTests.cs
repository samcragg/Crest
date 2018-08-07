namespace Host.UnitTests.Engine
{
    using System;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Xunit;

    public class HostingEnvironmentTests
    {
        public sealed class Constructor : HostingEnvironmentTests
        {
            private const string AspEnvironment = "ASPNETCORE_ENVIRONMENT";

            [Fact]
            public void ShouldDefaultToProductionIfNoEnvironmentIsSet()
            {
                Environment.SetEnvironmentVariable(AspEnvironment, null);

                var environment = new HostingEnvironment();

                environment.IsProduction.Should().BeTrue();
                environment.IsDevelopment.Should().BeFalse();
                environment.IsStaging.Should().BeFalse();
            }

            [Fact]
            public void ShouldDetectDevelopmentEnvironments()
            {
                Environment.SetEnvironmentVariable(AspEnvironment, "Development");
                var environment = new HostingEnvironment();

                environment.IsDevelopment.Should().BeTrue();
                environment.IsProduction.Should().BeFalse();
                environment.IsStaging.Should().BeFalse();
            }

            [Fact]
            public void ShouldDetectProductionEnvironments()
            {
                Environment.SetEnvironmentVariable(AspEnvironment, "Production");
                var environment = new HostingEnvironment();

                environment.IsProduction.Should().BeTrue();
                environment.IsDevelopment.Should().BeFalse();
                environment.IsStaging.Should().BeFalse();
            }

            [Fact]
            public void ShouldDetectStagingEnvironments()
            {
                Environment.SetEnvironmentVariable(AspEnvironment, "Staging");
                var environment = new HostingEnvironment();

                environment.IsStaging.Should().BeTrue();
                environment.IsDevelopment.Should().BeFalse();
                environment.IsProduction.Should().BeFalse();
            }
        }
    }
}
