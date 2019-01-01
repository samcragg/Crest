namespace IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Newtonsoft.Json;

    public sealed class WebFixture
    {
        private static readonly object LockObj = new object();
        private static readonly TestServer Server;

        static WebFixture()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Integration");

            var builder = new WebHostBuilder();
            builder.UseCrest();
            Server = new TestServer(builder);
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
            HttpClient client = this.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.ehDbi89WbFLgwiO45D8pNFARD2GBAwKogGhGf75YCw0");

            return client;
        }

        public HttpClient CreateClient()
        {
            lock (LockObj)
            {
                return Server.CreateClient();
            }
        }

        public async Task<HttpResponse> GetRawResponse(string url)
        {
            HttpContext context = await Server.SendAsync(c =>
            {
                c.Request.Path = url;
            });

            return context.Response;
        }

        public async Task<string> GetResultAsStringAsync(HttpClient client, HttpRequestMessage request)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            response.IsSuccessStatusCode.Should().BeTrue();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<string>(json);
        }

        public async Task<dynamic> GetResultAsync(HttpClient client, HttpRequestMessage request)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            response.IsSuccessStatusCode.Should().BeTrue();

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(json);
        }
    }
}
