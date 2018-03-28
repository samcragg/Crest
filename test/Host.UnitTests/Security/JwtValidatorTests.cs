namespace Host.UnitTests.Security
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using Crest.Host.Diagnostics;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JwtValidatorTests
    {
        private const int CurrentTimeInSeconds = 1483228800;
        private readonly DateTime currentTime = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly JwtValidationSettings settings = new JwtValidationSettings();
        private readonly ITimeProvider timeProvider = Substitute.For<ITimeProvider>();
        private readonly JwtValidator validator;

        protected JwtValidatorTests()
        {
            this.timeProvider.GetUtc().Returns(this.currentTime);
            this.validator = new JwtValidator(this.timeProvider, this.settings);
        }

        public sealed class GetValidClaimsPrincipal : JwtValidatorTests
        {
            [Fact]
            public void ShouldAdjustTheExpirationByTheClockScew()
            {
                this.settings.ClockSkew = TimeSpan.FromSeconds(5);

                ClaimsPrincipal result = this.GetPayload(@"""exp"":" + (CurrentTimeInSeconds - 4));

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAdjustTheNotBeforeByTheClockScew()
            {
                this.settings.ClockSkew = TimeSpan.FromSeconds(5);

                ClaimsPrincipal result = this.GetPayload(@"""nbf"":" + (CurrentTimeInSeconds + 4));

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowKnownIssuers()
            {
                this.settings.Issuers.Add("known issuer");

                ClaimsPrincipal result = this.GetPayload(@"""iss"":""known issuer""");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowMultipleAudienceValues()
            {
                this.settings.Audiences.Add("two");

                ClaimsPrincipal result = this.GetPayload(@"""aud"":[""one"", ""two""]");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowSingleAudienceValues()
            {
                this.settings.Audiences.Add("single");

                ClaimsPrincipal result = this.GetPayload(@"""aud"":""single""");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowTimesEqualToThecurrentTime()
            {
                this.settings.ClockSkew = TimeSpan.Zero;

                ClaimsPrincipal result = this.GetPayload(@"""nbf"":" + CurrentTimeInSeconds);

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldIncludeTheOriginalClaimIfNotMapped()
            {
                ClaimsPrincipal result = this.GetPayload(@"""original"":""value""");

                result.Claims.Should().ContainSingle(c => c.Type == "original")
                      .Which.Value.Should().Be("value");
            }

            [Fact]
            public void ShouldIncludeTheOriginalJwtClaimInTheProperties()
            {
                this.settings.JwtClaimMappings.Add("jwt", "mapped");

                ClaimsPrincipal result = this.GetPayload(@"""jwt"":""value""");

                result.Claims.Single(c => c.Type == "mapped")
                      .Properties.Should().ContainKey(JwtValidationSettings.JwtClaimProperty)
                      .WhichValue.Should().Be("jwt");
            }

            [Fact]
            public void ShouldMapTheJwtClaims()
            {
                this.settings.JwtClaimMappings.Add("jwt", "mapped");

                ClaimsPrincipal result = this.GetPayload(@"""jwt"":""value""");

                result.Claims.Should().ContainSingle(c => c.Type == "mapped")
                      .Which.Value.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnNullForInvaidExpirationTimes()
            {
                ClaimsPrincipal result = this.GetPayload(@"""exp"":""invalid_time""");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullForInvaidNotBeforeTimes()
            {
                ClaimsPrincipal result = this.GetPayload(@"""nbf"":""invalid_time""");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheAudienceIsUnknown()
            {
                ClaimsPrincipal result = this.GetPayload(@"""aud"":""unknown audience""");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheExpirationIsEqualToTheTime()
            {
                this.settings.ClockSkew = TimeSpan.Zero;

                ClaimsPrincipal result = this.GetPayload(@"""exp"":" + CurrentTimeInSeconds);

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheExpirationIsTooLate()
            {
                this.settings.ClockSkew = TimeSpan.Zero;

                ClaimsPrincipal result = this.GetPayload(@"""exp"":" + (CurrentTimeInSeconds - 1));

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheIssuerIsUnknown()
            {
                ClaimsPrincipal result = this.GetPayload(@"""iss"":""unknown issuer""");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheNotBeforeIsTooEarly()
            {
                this.settings.ClockSkew = TimeSpan.Zero;

                ClaimsPrincipal result = this.GetPayload(@"""nbf"":" + (CurrentTimeInSeconds + 1));

                result.Should().BeNull();
            }

            private ClaimsPrincipal GetPayload(string value)
            {
                byte[] payload = Encoding.UTF8.GetBytes("{" + value + "}");
                return this.validator.GetValidClaimsPrincipal(payload);
            }
        }
    }
}
