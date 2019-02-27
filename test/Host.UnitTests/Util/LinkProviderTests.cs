namespace Host.UnitTests.Util
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host.Util;
    using FluentAssertions;
    using Xunit;

    public class LinkProviderTests
    {
        private readonly LinkProvider provider = new LinkProvider();

        private interface IFakeService
        {
            [Get("/route/{id}"), Version(1)]
            Task Method(string id);
        }

        public sealed class Calling : LinkProviderTests
        {
            [Fact]
            public void ShouldSetTheHRef()
            {
                Link result = this.provider
                    .LinkTo<IFakeService>("x")
                    .Calling(x => x.Method("test value"))
                    .Build();

                result.HRef.OriginalString.Should().BeEquivalentTo("/v1/route/test%20value");
            }

            [Fact]
            public void ShouldSetTheRelationType()
            {
                Link result = this.provider
                    .LinkTo<IFakeService>("relation")
                    .Calling(x => x.Method(""))
                    .Build();

                result.RelationType.Should().Be("relation");
            }
        }
    }
}
