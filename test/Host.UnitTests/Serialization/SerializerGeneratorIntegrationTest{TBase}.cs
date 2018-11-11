namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    // This attribute is inherited to prevent the tests running in parallel
    // due to the static ModuleBuilder property
    [Collection(nameof(SerializerGenerator.ModuleBuilder))]
    [Trait("Category", "Integration")]
    public abstract class SerializerGeneratorIntegrationTest<TBase>
    {
        protected SerializerGeneratorIntegrationTest()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnitTestDynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            SerializerGenerator.ModuleBuilder = assemblyBuilder.DefineDynamicModule("Integration");
            this.Generator = new SerializerGenerator<TBase>();
        }

        public enum TestEnum
        {
            Value = 1
        }

        private protected ISerializerGenerator<TBase> Generator { get; }

        protected T Deserialize<T>(string input)
        {
            Type serializerType = this.Generator.GetSerializerFor(typeof(T));

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (var ms = new MemoryStream(bytes, writable: false))
            {
                var serializer = (ITypeSerializer)Activator.CreateInstance(serializerType, ms, SerializationMode.Deserialize);
                if (typeof(T).IsArray)
                {
                    return (T)((object)serializer.ReadArray());
                }
                else
                {
                    return (T)serializer.Read();
                }
            }
        }

        protected void ShouldSerializeAs<T>(T value, string expected)
        {
            string result = this.GetOutput(value);
            result = this.StripNonEssentialInformation(result);
            result = Regex.Replace(result, @"\s+", "");

            expected = Regex.Replace(expected, @"\s+", "");
            result.Should().BeEquivalentTo(expected);
        }

        protected virtual string StripNonEssentialInformation(string result)
        {
            return result;
        }

        private string GetOutput<T>(T value)
        {
            Type serializerType = this.Generator.GetSerializerFor(typeof(T));
            using (var ms = new MemoryStream())
            {
                var serializer = (ITypeSerializer)Activator.CreateInstance(serializerType, ms, SerializationMode.Serialize);

                if (typeof(T).IsArray)
                {
                    serializer.WriteArray((Array)((object)value));
                }
                else
                {
                    serializer.Write(value);
                }

                serializer.Flush();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public class FullClass
        {
            // Note: We're putting the Enum and Integer properties at the start
            // of the serialized information as they will always be outputted

            public SimpleClass Class { get; set; }

            public SimpleClass[] ClassArray { get; set; }

            [DataMember(Order = 1)]
            public TestEnum Enum { get; set; }

            public TestEnum[] EnumArray { get; set; }

            [DataMember(Order = 2)]
            public int Integer { get; set; }

            public int[] IntegerArray { get; set; }

            public int? NullableInteger { get; set; }

            public int?[] NullableIntegerArray { get; set; }

            public string String { get; set; }

            public string[] StringArray { get; set; }

            public Uri Uri { get; set; }
        }

        public class SimpleClass
        {
            public int Integer { get; set; }
        }
    }
}
