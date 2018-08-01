namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class LabelUnitTests
    {
        public sealed class Format : LabelUnitTests
        {
            [Fact]
            public void ShouldReturnTheValueWithTheLabel()
            {
                var unit = new LabelUnit("label");

                string result = unit.Format(123);

                result.Should().Be("123label");
            }
        }

        public sealed class ValueDescription : LabelUnitTests
        {
            [Fact]
            public void ShouldReturnTheLabelPassedInTheConstructor()
            {
                var unit = new LabelUnit("label text");

                string result = unit.ValueDescription;

                result.Should().Be("label text");
            }
        }
    }
}
