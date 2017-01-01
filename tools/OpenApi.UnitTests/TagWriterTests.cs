namespace OpenApi.UnitTests
{
    using System.IO;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NSubstitute;
    using NUnit.Framework;
    using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

    [TestFixture]
    public sealed class TagWriterTests
    {
        private XmlDocParser xmlDoc;

        [SetUp]
        public void SetUp()
        {
            this.xmlDoc = Substitute.For<XmlDocParser>();
        }

        [Test]
        public void CreateTagShouldUseTheDescriptionAttributeForTheTagName()
        {
            var writer = new TagWriter(this.xmlDoc, null);

            string result = writer.CreateTag(typeof(IWithDescription));

            Assert.That(result, Is.EqualTo("DescriptionText"));
        }

        [Test]
        public void CreateTagShouldUseTheeInterfaceName()
        {
            var writer = new TagWriter(this.xmlDoc, null);

            string result = writer.CreateTag(typeof(IFakeInterface));

            Assert.That(result, Is.EqualTo("FakeInterface"));
        }

        [Test]
        public void WriteTagsShouldOutputTheTagName()
        {
            dynamic result = this.GetOutput<IFakeInterface>();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That((string)result[0].name, Is.EqualTo("FakeInterface"));
        }

        [Test]
        public void WriteTagsShouldOutputTheInterfaceSummaryWithoutTrailingFullstop()
        {
            this.xmlDoc.GetClassDescription(typeof(IFakeInterface))
                .Returns(new ClassDescription { Summary = "Summary information." });

            dynamic result = this.GetOutput<IFakeInterface>();

            Assert.That((string)result[0].description, Is.EqualTo("Summary information"));
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

        private interface IFakeInterface
        {
        }

        [Description("DescriptionText")]
        private interface IWithDescription
        {
        }
    }
}
