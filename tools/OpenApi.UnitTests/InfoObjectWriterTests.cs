namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InfoObjectWriterTests
    {
        private const string GeneratedAssemblyName = "AssemblyName";

        [Test]
        public void ShouldOutputTheAssemblyNameIfThereIsNoTitleAttribute()
        {
            dynamic result = this.GetInformation(1, CreateAssembly());

            Assert.That((string)result.title, Is.EqualTo(GeneratedAssemblyName));
        }

        [Test]
        public void ShouldOutputTheVersion()
        {
            dynamic result = this.GetInformation(2, CreateAssembly());

            Assert.That((string)result.version, Is.EqualTo("2"));
        }

        [Test]
        public void ShouldOutputTheAssemblyDescriptionAttribute()
        {
            var assembly = CreateAssembly(() => new AssemblyDescriptionAttribute("Assembly description"));

            dynamic result = this.GetInformation(1, assembly);

            Assert.That((string)result.description, Is.EqualTo("Assembly description"));
        }

        [Test]
        public void ShouldOutputTheAssemblyTitleAttribute()
        {
            var assembly = CreateAssembly(() => new AssemblyTitleAttribute("Assembly Title"));

            dynamic result = this.GetInformation(1, assembly);

            Assert.That((string)result.title, Is.EqualTo("Assembly Title"));
        }

        [Test]
        public void ShouldOutputTheLicenseName()
        {
            var assembly = CreateAssembly(() => new AssemblyMetadataAttribute("License", "License name"));

            dynamic result = this.GetInformation(1, assembly);

            Assert.That((string)result.license.name, Is.EqualTo("License name"));
        }

        [Test]
        public void ShouldOutputTheLicenseUrl()
        {
            var assembly = CreateAssembly(
                () => new AssemblyMetadataAttribute("License", "License name"),
                () => new AssemblyMetadataAttribute("LicenseUrl", "http://www.example.com"));

            dynamic result = this.GetInformation(1, assembly);

            Assert.That((string)result.license.url, Is.EqualTo("http://www.example.com"));
        }

        [Test]
        public void ShouldNotOutputTheLicenseUrlIfTheLicenseNameIsMissing()
        {
            var assembly = CreateAssembly(() => new AssemblyMetadataAttribute("LicenseUrl", "http://www.example.com"));

            dynamic result = this.GetInformation(1, assembly);

            Assert.That(result.license, Is.Null);
        }

        private Assembly CreateAssembly(params Expression<Func<Attribute>>[] attributes)
        {
            AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(GeneratedAssemblyName), AssemblyBuilderAccess.Run);
            foreach (Expression<Func<Attribute>> attribute in attributes)
            {
                var constructorCall = attribute.Body as NewExpression;
                ConstructorInfo constructor = constructorCall.Constructor;
                object[] arguments = constructorCall.Arguments
                                                    .Select(a => ((ConstantExpression)a).Value)
                                                    .ToArray();

                builder.SetCustomAttribute(new CustomAttributeBuilder(constructor, arguments));
            }

            return builder;
        }

        private dynamic GetInformation(int version, Assembly assembly)
        {
            using (var stringWriter = new StringWriter())
            {
                var infoObjectWriter = new InfoObjectWriter(stringWriter);
                infoObjectWriter.WriteInformation(version, assembly);
                return JsonConvert.DeserializeObject("{" + stringWriter.ToString() + "}");
            }
        }
    }
}
