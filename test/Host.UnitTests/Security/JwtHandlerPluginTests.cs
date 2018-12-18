namespace Host.UnitTests.Security
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JwtHandlerPluginTests
    {
        private const int Unauthorized = 401;
        private readonly JwtHandlerPlugin plugin;
        private readonly IScopedServiceRegister register;
        private readonly JwtValidator validator;
        private readonly JwtSignatureVerifier verifier;

        private JwtHandlerPluginTests()
        {
            this.register = Substitute.For<IScopedServiceRegister>();

            this.validator = Substitute.For<JwtValidator>(null, Substitute.For<IJwtSettings>());
            this.validator.IsEnabled.Returns(true);

            this.verifier = Substitute.For<JwtSignatureVerifier>(new object[] { null });

            this.plugin = new JwtHandlerPlugin(
                this.register,
                this.validator,
                this.verifier);
        }

        public sealed class Order : JwtHandlerPluginTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.plugin.Order
                    .Should().BePositive();
            }
        }

        public sealed class ProcessAsync : JwtHandlerPluginTests
        {
            private static readonly MethodInfo AnonymousMethod =
                typeof(ProcessAsync).GetMethod(nameof(Anonymous), BindingFlags.NonPublic | BindingFlags.Static);

            private static readonly MethodInfo AuthenticatedMethod =
                typeof(ProcessAsync).GetMethod(nameof(Authenticated), BindingFlags.NonPublic | BindingFlags.Static);

            [Fact]
            public async Task ShouldAllowAnonymousMethods()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Handler.Returns(x => AnonymousMethod);

                IResponseData response = await this.plugin.ProcessAsync(request);

                response.Should().BeNull();
            }

            [Fact]
            public async Task ShouldNotAllowDifferentAuthorizationTypes()
            {
                // Example taken from RFC 7617
                IRequestData request = CreateRequest("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==");

                IResponseData response = await this.plugin.ProcessAsync(request);

                response.StatusCode.Should().Be(Unauthorized);
            }

            [Fact]
            public async Task ShouldNotAllowInvalidPayloads()
            {
                IRequestData request = CreateRequest("Bearer 123");
                this.verifier.IsSignatureValid(null, out byte[] _)
                    .ReturnsForAnyArgs(true);
                this.validator.GetValidClaimsPrincipal(null)
                    .ReturnsForAnyArgs((ClaimsPrincipal)null);

                IResponseData response = await this.plugin.ProcessAsync(request);

                response.StatusCode.Should().Be(Unauthorized);
            }

            [Fact]
            public async Task ShouldNotAllowInvalidSignatures()
            {
                IRequestData request = CreateRequest("Bearer 123");
                this.verifier.IsSignatureValid("123", out Arg.Any<byte[]>())
                    .Returns(false);

                IResponseData response = await this.plugin.ProcessAsync(request);

                // Make sure we got the right response and that we didn't try
                // to get the claims from an invalid JWT
                response.StatusCode.Should().Be(Unauthorized);
                this.validator.DidNotReceiveWithAnyArgs()
                    .GetValidClaimsPrincipal(null);
            }

            [Fact]
            public async Task ShouldNotAllowMissingAuthorizationHeader()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Handler.Returns(x => AuthenticatedMethod);

                IResponseData response = await this.plugin.ProcessAsync(request);

                response.StatusCode.Should().Be(Unauthorized);
            }

            [Fact]
            public async Task ShouldRegisterTheClaimsPrincipal()
            {
                var principal = new ClaimsPrincipal();
                IRequestData request = CreateRequestForPrincipal(principal);

                IResponseData response = await this.plugin.ProcessAsync(request);

                response.Should().BeNull();
                this.register.Received().UseInstance(typeof(IPrincipal), principal);
            }

            [Fact]
            public async Task ShouldSetTheGlobalClaimsPrincipal()
            {
                var principal = new ClaimsPrincipal();
                IRequestData request = CreateRequestForPrincipal(principal);

                await this.plugin.ProcessAsync(request);

                ClaimsPrincipal.Current.Should().BeSameAs(principal);
            }

            [Fact]
            public async Task ShouldSkipAuthenticationIfTheValidatorIsNotEneabled()
            {
                this.validator.IsEnabled.Returns(false);

                IResponseData response = await this.plugin.ProcessAsync(CreateRequest(""));

                response.Should().BeNull();
            }

            [Fact]
            public async Task ShouldWriteUnauthorizedToTheResponseStream()
            {
                IResponseData response = await this.plugin.ProcessAsync(CreateRequest(""));

                using (var stream = new MemoryStream())
                {
                    await response.WriteBody(stream);
                    string body = Encoding.UTF8.GetString(stream.ToArray());

                    body.Should().MatchEquivalentOf("*unauthorized*");
                }
            }

            [AllowAnonymous]
            private static void Anonymous()
            {
            }

            private static void Authenticated()
            {
            }

            private static IRequestData CreateRequest(string authorization)
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Handler.Returns(x => AuthenticatedMethod);
                request.Headers.Returns(new Dictionary<string, string>
                {
                    { "Authorization", authorization }
                });

                return request;
            }

            private IRequestData CreateRequestForPrincipal(ClaimsPrincipal principal)
            {
                IRequestData request = CreateRequest("Bearer 123");
                byte[] payload = new byte[0];

                this.verifier.IsSignatureValid("123", out Arg.Any<byte[]>())
                    .Returns(ci =>
                    {
                        ci[1] = payload;
                        return true;
                    });

                this.validator.GetValidClaimsPrincipal(payload)
                    .Returns(principal);
                return request;
            }
        }
    }
}
