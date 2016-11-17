namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using NUnit.Framework;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;

    [TestFixture]
    public sealed class HttpContextProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IRouteMapper mapper;
        private HttpContextProcessor processor;

        [SetUp]
        public void SetUp()
        {
            this.bootstrapper = Substitute.For<Bootstrapper>();

            this.mapper = Substitute.For<IRouteMapper>();
            this.bootstrapper.GetService<IRouteMapper>().Returns(this.mapper);

            this.processor = new HttpContextProcessor(this.bootstrapper);
        }

        [Test]
        public async Task HandleRequestShouldReturn404IfNoRouteIsFound()
        {
            HttpContext context = CreateContext("http://localhost/unknown");

            await this.processor.HandleRequest(context);

            Assert.That(context.Response.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task HandleRequestShouldReturn200ForFoundRoutes()
        {
            HttpContext context = CreateContext("http://localhost/route");
            MethodInfo method = Substitute.For<MethodInfo>();
            this.mapper.GetAdapter(method).Returns(_ => Task.FromResult<object>(""));

            // We need to call the Arg.Any calls in the same order as the method
            // for NSubstitute to handle them
            ILookup<string, string> query = Arg.Any<ILookup<string, string>>();
            IReadOnlyDictionary<string, object> any = Arg.Any<IReadOnlyDictionary<string, object>>();
            this.mapper.Match("GET", "/route", query, out any)
                .Returns(method);

            await this.processor.HandleRequest(context);

            Assert.That(context.Response.StatusCode, Is.EqualTo(200));
        }

        private static HttpContext CreateContext(string urlString)
        {
            Uri url = new Uri(urlString);

            HttpContext context = Substitute.For<HttpContext>();
            context.Request.Host = new HostString(url.Host, url.Port);
            context.Request.Method = "GET";
            context.Request.Path = new PathString(url.AbsolutePath);
            context.Request.QueryString = new QueryString(url.Query);
            context.Request.Scheme = url.Scheme;
            return context;
        }
    }
}
