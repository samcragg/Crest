namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.AspNetCore;
    using Crest.Host.Conversion;
    using Crest.Host.Engine;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HttpContextProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IContentConverter converter;
        private IRouteMapper mapper;
        private HttpContextProcessor processor;

        [SetUp]
        public void SetUp()
        {
            this.converter = Substitute.For<IContentConverter>();
            this.mapper = Substitute.For<IRouteMapper>();

            IContentConverterFactory factory = Substitute.For<IContentConverterFactory>();
            factory.GetConverter(null).ReturnsForAnyArgs(this.converter);

            IServiceRegister serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.GetContentConverterFactory().Returns(factory);

            this.bootstrapper = Substitute.For<Bootstrapper>(serviceRegister);
            this.bootstrapper.RouteMapper.Returns(this.mapper);

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

        [Test]
        public async Task HandleRequestShouldWriteTheBodyToTheResponse()
        {
            object methodReturn = new object();
            HttpContext context = CreateContext("http://localhost/route");
            MethodInfo method = Substitute.For<MethodInfo>();
            this.mapper.GetAdapter(method).Returns(_ => Task.FromResult(methodReturn));

            // Write to the passed in stream so it will get copied to the out response
            this.converter.WriteTo(Arg.Do<Stream>(s => s.WriteByte(1)), methodReturn);

            IReadOnlyDictionary<string, object> notUsed;
            this.mapper.Match(null, null, null, out notUsed)
                .ReturnsForAnyArgs(method);

            await this.processor.HandleRequest(context);

            Assert.That(context.Response.Body.Length, Is.EqualTo(1));
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
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
