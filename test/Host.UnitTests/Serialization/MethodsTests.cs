namespace Host.UnitTests.Serialization
{
    using System.Collections;
    using System.Linq;
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
                var enumerable = (IEnumerable)this.methods.StreamWriter;
                int count = 0;
                foreach (object x in enumerable)
                {
                    count++;
                }

                count.Should().Be(StreamWriterWriteMethods);
            }
        }
    }
}
