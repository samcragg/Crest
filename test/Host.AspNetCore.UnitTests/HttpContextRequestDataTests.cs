namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using Xunit;

    public class HttpContextRequestDataTests
    {
        public sealed class Constructor : HttpContextRequestDataTests
        {
            [Fact]
            public void ShouldAssignTheHandlerProperty()
            {
                MethodInfo method = Substitute.For<MethodInfo>();

                var data = new HttpContextRequestData(method, null, CreateContext());

                data.Handler.Should().BeSameAs(method);
            }

            [Fact]
            public void ShouldAssignTheHeadersProperty()
            {
                var data = new HttpContextRequestData(null, null, CreateContext());

                data.Headers.Should().NotBeNull();
            }

            [Fact]
            public void ShouldAssignTheParametersProperty()
            {
                var parameters = new Dictionary<string, object>();

                var data = new HttpContextRequestData(null, parameters, CreateContext());

                data.Parameters.Should().BeSameAs(parameters);
            }

            [Fact]
            public void ShouldCreateTheUrlFromTheContext()
            {
                const string Url = "https://host:123/path?query";

                var data = new HttpContextRequestData(null, null, CreateContext(Url));

                data.Url.AbsoluteUri.Should().Be(Url);
            }

            private static HttpContext CreateContext(string urlString = "http://localhost")
            {
                Uri url = new Uri(urlString);
                HttpContext context = Substitute.For<HttpContext>();
                context.Request.Host = new HostString(url.Host, url.Port);
                context.Request.Path = new PathString(url.AbsolutePath);
                context.Request.QueryString = new QueryString(url.Query);
                context.Request.Scheme = url.Scheme;
                return context;
            }
        }
    }
}
