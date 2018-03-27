namespace Host.UnitTests.Security
{
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class SecurityKeyCacheTests
    {
        private readonly SecurityKeyCache cache;
        private readonly ISecurityKeyProvider provider = Substitute.For<ISecurityKeyProvider>();

        public SecurityKeyCacheTests()
        {
            this.cache = new SecurityKeyCache(new[] { this.provider });
        }

        public sealed class GetECParameters : SecurityKeyCacheTests
        {
            [Fact]
            public async Task ShouldReturnThePublicKeyInformation()
            {
                this.provider.GetCertificatesAsync()
                    .Returns(new[] { CertificateHelper.GetCertificate("TestEcCert.pfx") });

                await this.cache.UpdateCache();
                ECParameters parameters = this.cache.GetECParameters().Single();

                // D is the private key (which we don't want)
                parameters.D.Should().BeNull();

                // Q is the public key (which we do want)
                parameters.Q.X.Should().NotBeNull();
                parameters.Q.Y.Should().NotBeNull();
            }
        }

        public sealed class GetRsaParameters : SecurityKeyCacheTests
        {
            [Fact]
            public async Task ShouldReturnThePublicKeyInformation()
            {
                this.provider.GetCertificatesAsync()
                    .Returns(new[] { CertificateHelper.GetCertificate("TestRsaCert.pfx") });

                await this.cache.UpdateCache();
                RSAParameters parameters = this.cache.GetRsaParameters().Single();

                // D is the private exponent, which shouldn't be there
                parameters.D.Should().BeNull();

                // Public exponent, which should be there
                parameters.Exponent.Should().NotBeNull();
            }
        }

        public sealed class GetSecretKeys : SecurityKeyCacheTests
        {
            [Fact]
            public async Task ShouldReturnTheSecretKeys()
            {
                byte[] secret = Encoding.ASCII.GetBytes("secret");
                this.provider.GetSecretKeysAsync()
                    .Returns(new[] { secret });

                await this.cache.UpdateCache();
                byte[] result = this.cache.GetSecretKeys().Single();

                result.Should().BeSameAs(secret);
            }
        }

        public sealed class UpdateCache : SecurityKeyCacheTests
        {
            [Fact]
            public async Task ShouldUpdateTheCertificatesWhenTheVersionChanges()
            {
                this.provider.Version.Returns(123);
                await this.cache.UpdateCache();
                await this.provider.Received().GetCertificatesAsync();

                this.provider.ClearReceivedCalls();
                await this.cache.UpdateCache();
                await this.provider.DidNotReceive().GetCertificatesAsync();

                this.provider.Version.Returns(321);
                await this.cache.UpdateCache();
                await this.provider.Received().GetCertificatesAsync();
            }
        }
    }
}
