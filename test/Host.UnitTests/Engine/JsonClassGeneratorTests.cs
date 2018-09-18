namespace Host.UnitTests.Engine
{
    using System;
    using Crest.Core.Logging;
    using Crest.Host.Engine;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class JsonClassGeneratorTests
    {
        private readonly JsonClassGenerator generator = new JsonClassGenerator();

        public sealed class CreatePopulateMethod : JsonClassGeneratorTests
        {
            [Fact]
            public void ShouldAssignArrayProperties()
            {
                SimpleClass result = this.PopulateNewInstance(
                    @"{ ""arrayProperty"": [true, false] }");

                result.ArrayProperty.Should().Equal(true, false);
            }

            [Fact]
            public void ShouldAssignIntegerProperties()
            {
                SimpleClass result = this.PopulateNewInstance(
                    @"{ ""integerProperty"": 123 }");

                result.IntegerProperty.Should().Be(123);
            }

            [Fact]
            public void ShouldAssignNullablePropertiesWithNulls()
            {
                var result = new SimpleClass { NullableProperty = 123 };
                this.PopulateExistingInstance(
                    @"{ ""nullableProperty"": null }",
                    result);

                result.NullableProperty.Should().BeNull();
            }

            [Fact]
            public void ShouldAssignNullablePropertiesWithValues()
            {
                SimpleClass result = this.PopulateNewInstance(
                    @"{ ""nullableProperty"": 123 }");

                result.NullableProperty.Should().Be(123);
            }

            [Fact]
            public void ShouldAssignStringProperties()
            {
                SimpleClass result = this.PopulateNewInstance(
                    @"{ ""stringProperty"": ""string"" }");

                result.StringProperty.Should().Be("string");
            }

            [Fact]
            public void ShouldIgnoreConversionFailures()
            {
                using (FakeLogger.LogInfo logging = FakeLogger.MonitorLogging())
                {
                    Action action = () => this.PopulateNewInstance(
                        @"{ ""integerProperty"": ""abc"" }");

                    action.Should().NotThrow();

                    logging.LogLevel.Should().Be(LogLevel.Error);
                    logging.Message.Should().Contain("abc");
                }
            }

            [Fact]
            public void ShouldIgnoreUnknownProperties()
            {
                using (FakeLogger.LogInfo logging = FakeLogger.MonitorLogging())
                {
                    Action action = () => this.PopulateNewInstance(
                        @"{ ""unknownProperty"": 123 }");

                    action.Should().NotThrow();

                    logging.LogLevel.Should().Be(LogLevel.Error);
                    logging.Message.Should().Contain("unknownProperty");
                }
            }

            private SimpleClass PopulateExistingInstance(string json, SimpleClass instance)
            {
                Action<object> action = this.generator.CreatePopulateMethod(
                    typeof(SimpleClass),
                    json);

                action(instance);
                return instance;
            }

            private SimpleClass PopulateNewInstance(string json)
            {
                return this.PopulateExistingInstance(json, new SimpleClass());
            }
        }

        private class SimpleClass
        {
            public bool[] ArrayProperty { get; set; }
            public int IntegerProperty { get; set; }
            public int? NullableProperty { get; set; }
            public string StringProperty { get; set; }
        }
    }
}
