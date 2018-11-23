namespace Core.UnitTests
{
    using System;
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class LinkBuilderTests
    {
        private readonly LinkBuilder builder;

        private LinkBuilderTests()
        {
            this.builder = new LinkBuilder("relation", new Uri("http://www.example.com"));
        }

        public sealed class WithDeprecation : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                var deprecationUrl = new Uri("http://www.example.com/deprecation-info");
                this.builder.WithDeprecation(deprecationUrl);

                Link result = this.builder.Build();

                result.Deprecation.Should().Be(deprecationUrl);
            }
        }

        public sealed class WithHRefLang : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                this.builder.WithHRefLang("language");

                Link result = this.builder.Build();

                result.HRefLang.Should().Be("language");
            }
        }

        public sealed class WithName : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                this.builder.WithName("name");

                Link result = this.builder.Build();

                result.Name.Should().Be("name");
            }
        }

        public sealed class WithProfile : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                var profileUrl = new Uri("http://www.example.com/profile");
                this.builder.WithProfile(profileUrl);

                Link result = this.builder.Build();

                result.Profile.Should().Be(profileUrl);
            }
        }

        public sealed class WithTemplated : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                this.builder.WithTemplated(true);

                Link result = this.builder.Build();

                result.Templated.Should().BeTrue();
            }
        }

        public sealed class WithTitle : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                this.builder.WithTitle("title");

                Link result = this.builder.Build();

                result.Title.Should().Be("title");
            }
        }

        public sealed class WithType : LinkBuilderTests
        {
            [Fact]
            public void ShouldSetTheTypeProperty()
            {
                this.builder.WithType("type");

                Link result = this.builder.Build();

                result.Type.Should().Be("type");
            }
        }
    }
}
