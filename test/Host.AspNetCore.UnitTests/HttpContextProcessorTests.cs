namespace Host.AspNetCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host;
    using Crest.Host.AspNetCore;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using NSubstitute.ReturnsExtensions;
    using Xunit;

    public class HttpContextProcessorTests
    {
        private Bootstrapper bootstrapper;
        private IContentConverter converter;
        private IRouteMapper mapper;
        private HttpContextProcessor processor;

        protected HttpContextProcessorTests()
        {
            this.converter = Substitute.For<IContentConverter>();
            this.mapper = Substitute.For<IRouteMapper>();
            this.mapper.FindOverride(null, null).ReturnsNullForAnyArgs();

            IContentConverterFactory factory = Substitute.For<IContentConverterFactory>();
            factory.GetConverter(null).ReturnsForAnyArgs(this.converter);

            IServiceLocator serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.GetContentConverterFactory().Returns(factory);

            this.bootstrapper = Substitute.For<Bootstrapper>(serviceLocator);
            this.bootstrapper.RouteMapper.Returns(this.mapper);

            this.processor = new HttpContextProcessor(this.bootstrapper);
        }

        public sealed class HandleRequest : HttpContextProcessorTests
        {
            [Fact]
            public async Task ShouldReturn200ForFoundRoutes()
            {
                HttpContext context = CreateContext("http://localhost/route");
                MethodInfo method = Substitute.For<MethodInfo>();
                this.mapper.GetAdapter(method).Returns(_ => Task.FromResult<object>(""));

                // We need to call the Arg.Any calls in the same order as the method
                // for NSubstitute to handle them
                ILookup<string, string> query = Arg.Any<ILookup<string, string>>();
                IReadOnlyDictionary<string, object> any = Arg.Any<IReadOnlyDictionary<string, object>>();
                this.mapper.Match("GET", "/route", query, out any)
                    .Returns(ci =>
                    {
                        ci[3] = new Dictionary<string, object>();
                        return method;
                    });

                await this.processor.HandleRequest(context);

                context.Response.StatusCode.Should().Be(200);
            }

            [Fact]
            public async Task ShouldWriteTheBodyToTheResponse()
            {
                object methodReturn = new object();
                HttpContext context = CreateContext("http://localhost/route");
                MethodInfo method = Substitute.For<MethodInfo>();
                this.mapper.GetAdapter(method).Returns(_ => Task.FromResult(methodReturn));

                // Write to the passed in stream so it will get copied to the out response
                this.converter.WriteTo(Arg.Do<Stream>(s => s.WriteByte(1)), methodReturn);

                this.mapper.Match(null, null, null, out _)
                    .ReturnsForAnyArgs(ci =>
                    {
                        ci[3] = new Dictionary<string, object>();
                        return method;
                    });

                await this.processor.HandleRequest(context);

                context.Response.Body.Length.Should().Be(1);
            }

            [Fact]
            public async Task ShouldWriteTheHeadersToTheResponse()
            {
                HttpContext context = CreateContext("http://localhost/route");
                OverrideMethod method = (r, c) =>
                {
                    IResponseData response = Substitute.For<IResponseData>();
                    response.Headers.Returns(new Dictionary<string, string>
                    {
                        { "Header", "Value" }
                    });

                    return Task.FromResult(response);
                };

                this.mapper.FindOverride("GET", "/route")
                    .Returns(method);

                await this.processor.HandleRequest(context);

                context.Response.Headers["Header"].ToString()
                       .Should().Be("Value");
            }

            private static HttpContext CreateContext(string urlString)
            {
                var url = new Uri(urlString);

                HttpContext context = Substitute.For<HttpContext>();
                context.Request.Host = new HostString(url.Host, url.Port);
                context.Request.Method = "GET";
                context.Request.Path = new PathString(url.AbsolutePath);
                context.Request.QueryString = new QueryString(url.Query);
                context.Request.Scheme = url.Scheme;
                context.Response.Body = new MemoryStream();
                context.Response.Headers.Returns(new HeaderDictionary());
                return context;
            }
        }
    }
}
