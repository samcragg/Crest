namespace Host.UnitTests.Serialization
{
    using System.Collections;
    using System.Linq;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class MethodsTests
    {
        private readonly Methods methods = new Methods();

        public sealed class ValueWriter : MethodsTests
        {
            private const int ValueWriterWriteMethods = 18;

            [Fact]
            public void ShouldEnumerateAllTheWriteMethods()
            {
                int count = this.methods.ValueWriter.Count();

                count.Should().Be(ValueWriterWriteMethods);
            }

            [Fact]
            public void ShouldProvideANonGenericEnumerateMethod()
            {
                var enumerable = (IEnumerable)this.methods.ValueWriter;
                int count = 0;
                foreach (object x in enumerable)
                {
                    count++;
                }

                count.Should().Be(ValueWriterWriteMethods);
            }
        }
    }
}
