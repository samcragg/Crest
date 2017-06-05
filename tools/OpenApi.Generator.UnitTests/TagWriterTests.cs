namespace OpenApi.Generator.UnitTests
{
    using System.Collections;
    using System.ComponentModel;
    using System.IO;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class TagWriterTests
    {
        private readonly XmlDocParser xmlDoc = Substitute.For<XmlDocParser>();

        private interface IFakeInterface
        {
        }

        public sealed class CreateTag : TagWriterTests
        {
            [Description("DescriptionText")]
            private interface IWithDescription
            {
            }

            [Fact]
            public void ShouldUseTheDescriptionAttributeForTheTagName()
            {
                var writer = new TagWriter(this.xmlDoc, null);

                string result = writer.CreateTag(typeof(IWithDescription));

                result.Should().Be("DescriptionText");
            }

            [Fact]
            public void ShouldUseTheeInterfaceName()
            {
                var writer = new TagWriter(this.xmlDoc, null);

                string result = writer.CreateTag(typeof(IFakeInterface));

                result.Should().Be("FakeInterface");
            }
        }

        public sealed class WriteTags : TagWriterTests
        {
            [Fact]
            public void ShouldOutputTheInterfaceSummaryWithoutTrailingFullstop()
            {
                this.xmlDoc.GetClassDescription(typeof(IFakeInterface))
                    .Returns(new ClassDescription { Summary = "Summary information." });

                dynamic result = this.GetOutput<IFakeInterface>();

                ((string)result[0].description).Should().Be("Summary information");
            }

            [Fact]
            public void WriteTagsShouldOutputTheTagName()
            {
                dynamic result = this.GetOutput<IFakeInterface>();

                ((IEnumerable)result).Should().HaveCount(1);
                ((string)result[0].name).Should().Be("FakeInterface");
            }

            private dynamic GetOutput<T>()
            {
                using (var stringWriter = new StringWriter())
                {
                    var tagWriter = new TagWriter(this.xmlDoc, stringWriter);
                    tagWriter.CreateTag(typeof(T));
                    tagWriter.WriteTags();

                    return JsonConvert.DeserializeObject(stringWriter.ToString());
                }
            }
        }
    }
}
