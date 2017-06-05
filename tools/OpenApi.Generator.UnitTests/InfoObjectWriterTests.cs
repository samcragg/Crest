namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.OpenApi;
    using FluentAssertions;
    using Newtonsoft.Json;
    using Xunit;

    public class InfoObjectWriterTests
    {
        private const string GeneratedAssemblyName = "AssemblyName";

        public sealed class WriteInformation : InfoObjectWriterTests
        {
            [Fact]
            public void ShouldNotOutputTheLicenseUrlIfTheLicenseNameIsMissing()
            {
                Assembly assembly = CreateAssembly(() => new AssemblyMetadataAttribute("LicenseUrl", "http://www.example.com"));

                dynamic result = this.GetInformation(1, assembly);

                ((object)result.license).Should().BeNull();
            }

            [Fact]
            public void ShouldOutputTheAssemblyDescriptionAttribute()
            {
                Assembly assembly = CreateAssembly(() => new AssemblyDescriptionAttribute("Assembly description"));

                dynamic result = this.GetInformation(1, assembly);

                ((string)result.description).Should().Be("Assembly description");
            }

            [Fact]
            public void ShouldOutputTheAssemblyNameIfThereIsNoTitleAttribute()
            {
                dynamic result = this.GetInformation(1, CreateAssembly());

                ((string)result.title).Should().Be(GeneratedAssemblyName);
            }

            [Fact]
            public void ShouldOutputTheAssemblyTitleAttribute()
            {
                Assembly assembly = CreateAssembly(() => new AssemblyTitleAttribute("Assembly Title"));

                dynamic result = this.GetInformation(1, assembly);

                ((string)result.title).Should().Be("Assembly Title");
            }

            [Fact]
            public void ShouldOutputTheLicenseName()
            {
                Assembly assembly = CreateAssembly(() => new AssemblyMetadataAttribute("License", "License name"));

                dynamic result = this.GetInformation(1, assembly);

                ((string)result.license.name).Should().Be("License name");
            }

            [Fact]
            public void ShouldOutputTheLicenseUrl()
            {
                Assembly assembly = CreateAssembly(
                    () => new AssemblyMetadataAttribute("License", "License name"),
                    () => new AssemblyMetadataAttribute("LicenseUrl", "http://www.example.com"));

                dynamic result = this.GetInformation(1, assembly);

                ((string)result.license.url).Should().Be("http://www.example.com");
            }

            [Fact]
            public void ShouldOutputTheVersion()
            {
                dynamic result = this.GetInformation(2, CreateAssembly());

                ((string)result.version).Should().Be("2");
            }

            private Assembly CreateAssembly(params Expression<Func<Attribute>>[] attributes)
            {
                var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(GeneratedAssemblyName), AssemblyBuilderAccess.Run);
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
}
