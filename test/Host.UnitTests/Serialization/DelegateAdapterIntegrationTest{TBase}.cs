namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Crest.Host.Engine;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;
    using IFormatter = Crest.Host.Serialization.Internal.IFormatter;

    [Trait("Category", "Integration")]
    public abstract class DelegateAdapterIntegrationTest
    {
        // Since the formatters are internal, we can't use them as a generic
        // parameter as our test classes need to be public
        private readonly Type formatterType;

        protected DelegateAdapterIntegrationTest(Type formatterType)
        {
            this.formatterType = formatterType;
        }

        public enum TestEnum
        {
            Value = 1
        }

        protected List<Type> DiscoveredTypes { get; } = new List<Type>();

        protected T Deserialize<T>(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (var ms = new MemoryStream(bytes, writable: false))
            {
                return (T)this.WithGenerator(x => x.Deserialize(ms, typeof(T)));
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
            using (var ms = new MemoryStream())
            {
                this.WithGenerator(x => x.Serialize(ms, value));
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private object WithGenerator(Expression<Action<ISerializerGenerator<IFormatter>>> expression)
        {
            Type adapterType = typeof(DelegateAdapter<>).MakeGenericType(this.formatterType);
            ConstructorInfo constructor = adapterType.GetConstructor(new[] { typeof(DiscoveredTypes) });

            Expression discoveredType = Expression.Constant(new DiscoveredTypes(this.DiscoveredTypes));
            Expression instance = Expression.New(constructor, discoveredType);

            var methodCall = (MethodCallExpression)expression.Body;
            LambdaExpression lambda = Expression.Lambda(
                Expression.Call(
                    instance,
                    adapterType.GetMethod(methodCall.Method.Name),
                    methodCall.Arguments));

            return lambda.Compile().DynamicInvoke();
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
