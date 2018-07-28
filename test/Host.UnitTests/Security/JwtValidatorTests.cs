namespace Host.UnitTests.Security
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;
    using Crest.Host.Logging;
    using Crest.Host.Security;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using NSubstitute;
    using Xunit;

    public class JwtValidatorTests
    {
        private const int CurrentTimeInSeconds = 1483228800;
        private readonly DateTime currentTime = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly IJwtSettings settings = Substitute.For<IJwtSettings>();
        private readonly ITimeProvider timeProvider = Substitute.For<ITimeProvider>();
        private readonly JwtValidator validator;

        protected JwtValidatorTests()
        {
            this.timeProvider.GetUtc().Returns(this.currentTime);
            this.validator = new JwtValidator(this.timeProvider, this.settings);
        }

        public sealed class Constructor : JwtValidatorTests
        {
            [Fact]
            public void ShouldLogAWarningIfJwtValidationIsDisabled()
            {
                using (FakeLogger.LogInfo logging = FakeLogger.MonitorLogging())
                {
                    this.settings.SkipAuthentication.Returns(true);
                    new JwtValidator(this.timeProvider, this.settings);

                    logging.LogLevel.Should().Be(LogLevel.Warn);
                    logging.Message.Should().NotBeNullOrEmpty();
                }
            }
        }

        public sealed class GetValidClaimsPrincipal : JwtValidatorTests
        {
            [Fact]
            public void ShouldAdjustTheExpirationByTheClockScew()
            {
                this.settings.ClockSkew.Returns(TimeSpan.FromSeconds(5));

                ClaimsPrincipal result = this.GetPayload(@"""exp"":" + (CurrentTimeInSeconds - 4));

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAdjustTheNotBeforeByTheClockScew()
            {
                this.settings.ClockSkew.Returns(TimeSpan.FromSeconds(5));

                ClaimsPrincipal result = this.GetPayload(@"""nbf"":" + (CurrentTimeInSeconds + 4));

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowKnownIssuers()
            {
                this.settings.Issuers.Returns(new HashSet<string>(new[] { "known issuer" }));

                ClaimsPrincipal result = this.GetPayload(@"""iss"":""known issuer""");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowMultipleAudienceValues()
            {
                this.settings.Audiences.Returns(new HashSet<string>(new[] { "two" }));

                ClaimsPrincipal result = this.GetPayload(@"""aud"":[""one"", ""two""]");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowSingleAudienceValues()
            {
                this.settings.Audiences.Returns(new HashSet<string>(new[] { "single" }));

                ClaimsPrincipal result = this.GetPayload(@"""aud"":""single""");

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAllowTimesEqualToThecurrentTime()
            {
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
                string any = Arg.Any<string>();
                this.settings.JwtClaimMappings.TryGetValue("jwt", out any)
                    .Returns(ci =>
                    {
                        ci[1] = "mapped";
                        return true;
                    });

                ClaimsPrincipal result = this.GetPayload(@"""jwt"":""value""");

                result.Claims.Single(c => c.Type == "mapped")
                      .Properties.Should().ContainKey(JwtValidationSettings.JwtClaimProperty)
                      .WhichValue.Should().Be("jwt");
            }

            [Fact]
            public void ShouldMapTheJwtClaims()
            {
                string any = Arg.Any<string>();
                this.settings.JwtClaimMappings.TryGetValue("jwt", out any)
                    .Returns(ci =>
                    {
                        ci[1] = "mapped";
                        return true;
                    });

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
                ClaimsPrincipal result = this.GetPayload(@"""exp"":" + CurrentTimeInSeconds);

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfTheExpirationIsTooLate()
            {
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
                ClaimsPrincipal result = this.GetPayload(@"""nbf"":" + (CurrentTimeInSeconds + 1));

                result.Should().BeNull();
            }

            private ClaimsPrincipal GetPayload(string value)
            {
                byte[] payload = Encoding.UTF8.GetBytes("{" + value + "}");
                return this.validator.GetValidClaimsPrincipal(payload);
            }
        }

        public sealed class IsEnabled : JwtValidatorTests
        {
            [Fact]
            public void ShouldReturnFalseIfSkipAuthorizationIsTrue()
            {
                this.settings.SkipAuthentication.Returns(true);

                bool result = this.validator.IsEnabled;

                result.Should().BeFalse();
            }
        }
    }
}
