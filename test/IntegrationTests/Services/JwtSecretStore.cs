namespace IntegrationTests.Services
{
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Host.Security;

    public sealed class JwtSecretStore : ISecurityKeyProvider
    {
        internal const string SecretText = "integrations_tests";

        public int Version => 1;

        public Task<X509Certificate2[]> GetCertificatesAsync()
        {
            return Task.FromResult(new X509Certificate2[0]);
        }

        public Task<byte[][]> GetSecretKeysAsync()
        {
            return Task.FromResult(new[]
            {
                Encoding.ASCII.GetBytes(SecretText)
            });
        }
    }
}
