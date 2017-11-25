namespace Host.UnitTests.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class MethodsTests
    {
        private readonly Methods methods = new Methods(typeof(FakeSerializerBase));

        public sealed class StreamWriter : MethodsTests
        {
            private const int StreamWriterWriteMethods = 17;

            [Fact]
            public void ShouldEnumerateAllTheWriteMethods()
            {
                int count = this.methods.StreamWriter.Count();

                count.Should().Be(StreamWriterWriteMethods);
            }

            [Fact]
            public void ShouldProvideANonGenericEnumerateMethod()
            {
                int count = ((IEnumerable)this.methods.StreamWriter)
                    .Cast<KeyValuePair<Type, MethodInfo>>()
                    .Count();

                count.Should().Be(StreamWriterWriteMethods);
            }
        }
    }
}
