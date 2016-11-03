namespace Core.UnitTests
{
    using Crest.Core;
    using NUnit.Framework;

    [TestFixture]
    public sealed class VersionAttributeTests
    {
        [Test]
        public void ConstructorShouldSetTheFromProperty()
        {
            var instance = new VersionAttribute(123);

            Assert.That(instance.From, Is.EqualTo(123));
        }

        [Test]
        public void ConstructorShouldSetTheToProperty()
        {
            var instance = new VersionAttribute(0, 123);

            Assert.That(instance.To, Is.EqualTo(123));
        }

        [Test]
        public void ToShouldDefaultToIntMaxValue()
        {
            var instance = new VersionAttribute(0);

            Assert.That(instance.To, Is.EqualTo(int.MaxValue));
        }
    }
}
