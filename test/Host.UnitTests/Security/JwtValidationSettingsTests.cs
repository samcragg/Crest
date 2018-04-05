namespace Host.UnitTests.Security
{
    using Crest.Abstractions;
    using Crest.Host.Security;
    using FluentAssertions;
    using Xunit;

    public class JwtValidationSettingsTests
    {
        private readonly JwtValidationSettings settings = new JwtValidationSettings();

        public sealed class Audiences : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldReturnAnEmptyList()
            {
                this.settings.Audiences.Should().BeEmpty();
            }
        }

        public sealed class AuthenticationType : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldReturnFederation()
            {
                this.settings.AuthenticationType.Should().Match("*Federation*");
            }
        }

        public sealed class ClockSkew : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldDefaultToFiveMinutes()
            {
                this.settings.ClockSkew.TotalMinutes.Should().Be(5);
            }
        }

        public sealed class Issuers : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldReturnAnEmptyList()
            {
                this.settings.Issuers.Should().BeEmpty();
            }
        }

        public sealed class JwtClaimMappings : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldBeMutableForDerivedClasses()
            {
                this.settings.JwtClaimMappings.Add("1", "2");

                this.settings.JwtClaimMappings
                    .Should().ContainKey("1")
                    .WhichValue.Should().Be("2");
            }

            [Fact]
            public void ShouldIncludeTheUniqueClaim()
            {
                const string SoapClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

                ((IJwtSettings)this.settings).JwtClaimMappings["unique_name"]
                    .Should().Be(SoapClaim);
            }
        }

        public sealed class SkipAuthentication : JwtValidationSettingsTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                this.settings.SkipAuthentication.Should().BeFalse();
            }
        }
    }
}
