namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class HttpContextRequestDataTests
    {
        [Test]
        public void ShouldAssignTheHandlerProperty()
        {
            MethodInfo method = Substitute.For<MethodInfo>();

            var data = new HttpContextRequestData(method, null, CreateContext());

            Assert.That(data.Handler, Is.SameAs(method));
        }

        [Test]
        public void ShouldAssignTheParametersProperty()
        {
            var parameters = new Dictionary<string, object>();

            var data = new HttpContextRequestData(null, parameters, CreateContext());

            Assert.That(data.Parameters, Is.SameAs(parameters));
        }

        [Test]
        public void ShouldCreateTheUrlFromTheContext()
        {
            const string Url = "https://host:123/path?query";

            var data = new HttpContextRequestData(null, null, CreateContext(Url));

            Assert.That(data.Url.AbsoluteUri, Is.EqualTo(Url));
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
