namespace Host.UnitTests.Security
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RsaSignatureValidatorTests
    {
        private readonly SecurityKeyCache cache;
        private readonly RsaSignatureValidator validator;

        public RsaSignatureValidatorTests()
        {
            this.cache = Substitute.For<SecurityKeyCache>(new object[] { new ISecurityKeyProvider[0] });
            this.validator = new RsaSignatureValidator(this.cache);

            using (X509Certificate2 cert = CertificateHelper.GetCertificate("TestRsaCert.pfx"))
            {
                this.cache.GetRsaParameters().Returns(new[] { cert.GetRSAPublicKey().ExportParameters(false) });
            }
        }

        public sealed class AlgorithmPrefix : RsaSignatureValidatorTests
        {
            [Fact]
            public void ShouldReturnRS()
            {
                this.validator.AlgorithmPrefix
                    .Should().Be("RS");
            }
        }

        public sealed class IsValid : RsaSignatureValidatorTests
        {
            [Fact]
            public void ShouldReturnFalseForInvalidSignatures()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data);
                signature[0] = (byte)~signature[0];

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA256);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForValidSignatures()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data);

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA256);

                result.Should().BeTrue();
            }

            private static byte[] SignData(byte[] data)
            {
                using (X509Certificate2 cert = CertificateHelper.GetCertificate("TestRsaCert.pfx"))
                using (RSA rsa = cert.GetRSAPrivateKey())
                {
                    return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
        }
    }
}
