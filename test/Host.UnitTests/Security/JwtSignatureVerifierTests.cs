namespace Host.UnitTests.Security
{
    using System.Security.Cryptography;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JwtSignatureVerifierTests
    {
        private readonly ISignatureValidator validator = Substitute.For<ISignatureValidator>();
        private readonly JwtSignatureVerifier verifier;

        private JwtSignatureVerifierTests()
        {
            // Return true from the validator to prove that the other checks
            // are returning false.
            this.validator.AlgorithmPrefix.Returns("UT");
            this.validator.IsValid(null, null, default)
                .ReturnsForAnyArgs(true);

            this.verifier = new JwtSignatureVerifier(new[] { this.validator });
        }

        public sealed class IsSignatureValid : JwtSignatureVerifierTests
        {
            [Fact]
            public void ShouldAllowMissingTypeInformation()
            {
                // {"alg":"UT256"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDI1NiJ9.payload.signature", out _);

                this.validator.ReceivedWithAnyArgs()
                    .IsValid(null, null, default);
            }

            [Fact]
            public void ShouldHandleSha256HashedSignatures()
            {
                // {"alg":"UT256","typ":"JWT"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDI1NiIsInR5cCI6IkpXVCJ9.payload.signature", out _);

                this.validator.Received()
                    .IsValid(Arg.Any<byte[]>(), Arg.Any<byte[]>(), HashAlgorithmName.SHA256);
            }

            [Fact]
            public void ShouldHandleSha384HashedSignatures()
            {
                // {"alg":"UT384","typ":"JWT"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDM4NCIsInR5cCI6IkpXVCJ9.payload.signature", out _);

                this.validator.Received()
                    .IsValid(Arg.Any<byte[]>(), Arg.Any<byte[]>(), HashAlgorithmName.SHA384);
            }

            [Fact]
            public void ShouldHandleSha512HashedSignatures()
            {
                // {"alg":"UT512","typ":"JWT"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDUxMiIsInR5cCI6IkpXVCJ9.payload.signature", out _);

                this.validator.Received()
                    .IsValid(Arg.Any<byte[]>(), Arg.Any<byte[]>(), HashAlgorithmName.SHA512);
            }

            [Fact]
            public void ShouldOutputThePayload()
            {
                // {"alg":"UT256"}.payload
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDI1NiJ9.cGF5bG9hZA.signature", out byte[] payload);

                payload.Should().Equal(Encoding.ASCII.GetBytes("payload"));
            }

            [Fact]
            public void ShouldReturnFalseForInvalidBase64Headers()
            {
                this.verifier.IsSignatureValid("eyJ#.payload.signature", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForInvalidBase64Signatures()
            {
                // {"alg":"UT256","typ":"JWT"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDI1NiIsInR5cCI6IkpXVCJ9.payload.signature#", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForMissingAlgorithms()
            {
                // {"typ":"JWT"}
                this.verifier.IsSignatureValid("eyJ0eXAiOiJKV1QifQ.payload.signature", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForMissingPayloads()
            {
                this.verifier.IsSignatureValid("header", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForMissingSignatures()
            {
                this.verifier.IsSignatureValid("header.payload", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNonJwtTypes()
            {
                // {"alg":"UT256","typ":"unknown"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDI1NiIsInR5cCI6InVua25vd24ifQ.payload.signature", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNullTokens()
            {
                this.verifier.IsSignatureValid(null, out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForUnknownAlgorithms()
            {
                // {"alg":"XX123"}
                this.verifier.IsSignatureValid("eyJhbGciOiJYWDEyMyJ9.payload.signature", out _)
                    .Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForUnknownHashingAlgorithms()
            {
                // {"alg":"UT123"}
                this.verifier.IsSignatureValid("eyJhbGciOiJVVDEyMyJ9.payload.signature", out _)
                    .Should().BeFalse();
            }
        }
    }
}
