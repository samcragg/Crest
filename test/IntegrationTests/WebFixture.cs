namespace IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;

    public sealed class WebFixture : IDisposable
    {
        private readonly TestServer server;

        public WebFixture()
        {
            var builder = new WebHostBuilder();
            builder.UseCrest();
            this.server = new TestServer(builder);
        }

        public HttpClient CreateAuthenticatedClient()
        {
            // The JWT has been created by https://jwt.io using the HS256
            // algorithm (with JwtSecretStore.SecretText for the secret) and
            // the default body of:
            // {
            //   "sub": "1234567890",
            //   "name": "John Doe",
            //   "iat": 1516239022
            // }
            HttpClient client = this.server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.ehDbi89WbFLgwiO45D8pNFARD2GBAwKogGhGf75YCw0");

            return client;
        }

        public HttpClient CreateClient()
        {
            return this.server.CreateClient();
        }

        public void Dispose()
        {
            this.server.Dispose();
        }
    }
}
