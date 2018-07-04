namespace Host.UnitTests.Security
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ECSignatureValidatorTests
    {
        private readonly SecurityKeyCache cache;
        private readonly ECSignatureValidator validator;

        public ECSignatureValidatorTests()
        {
            this.cache = Substitute.For<SecurityKeyCache>();
            this.validator = new ECSignatureValidator(this.cache);

            using (X509Certificate2 cert = CertificateHelper.GetCertificate("TestEcCert.pfx"))
            {
                this.cache.GetECParameters().Returns(new[] { cert.GetECDsaPublicKey().ExportParameters(false) });
            }
        }

        public sealed class AlgorithmPrefix : ECSignatureValidatorTests
        {
            [Fact]
            public void ShouldReturnES()
            {
                this.validator.AlgorithmPrefix
                    .Should().Be("ES");
            }
        }

        public sealed class IsValid : ECSignatureValidatorTests
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
                using (X509Certificate2 cert = CertificateHelper.GetCertificate("TestEcCert.pfx"))
                using (ECDsa ec = cert.GetECDsaPrivateKey())
                {
                    return ec.SignData(data, HashAlgorithmName.SHA256);
                }
            }
        }
    }
}
