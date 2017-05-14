namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class QueryCaptureTests
    {
        private const string CapturedParameter = "parameter";
        private readonly Dictionary<Type, Func<string, IQueryValueConverter>> converters;
        private readonly ILookup<string, string> lookup = Substitute.For<ILookup<string, string>>();
        private IQueryValueConverter converter = new FakeConverter();

        protected QueryCaptureTests()
        {
            this.converters = new Dictionary<Type, Func<string, IQueryValueConverter>>
            {
                { typeof(int), _ => this.converter }
            };
        }

        protected void AddLookupItem(string key, params string[] values)
        {
            this.lookup[key].Returns(values);
        }

        public sealed class Create : QueryCaptureTests
        {
            [Fact]
            public void ShouldFallbackToUseTheGenericConverter()
            {
                FakeTypeConverter.CreatedCount = 0;

                QueryCapture.Create("", typeof(FakeClass), "", this.converters.TryGetValue);

                FakeTypeConverter.CreatedCount.Should().Be(1);
            }

            [Fact]
            public void ShouldPassTheParameterNameToTheConverter()
            {
                string parameterName = null;
                this.converters.Add(typeof(long), p => { parameterName = p; return this.converter; });

                QueryCapture.Create("key", typeof(long), "parameter", this.converters.TryGetValue);

                parameterName.Should().Be("parameter");
            }

            [Fact]
            public void ShouldUseTheSpecializedConverter()
            {
                this.converter = Substitute.For<IQueryValueConverter>(); // We need to spy on it
                this.AddLookupItem("key", "value");

                var result = QueryCapture.Create("key", typeof(int), "", this.converters.TryGetValue);
                result.ParseParameters(this.lookup, new Dictionary<string, object>());

                this.converter.ReceivedWithAnyArgs().TryConvertValue(default(StringSegment), out _);
            }

            [TypeConverter(typeof(FakeTypeConverter))]
            private sealed class FakeClass
            {
            }

            private sealed class FakeTypeConverter : TypeConverter
            {
                internal static int CreatedCount;

                public FakeTypeConverter()
                {
                    CreatedCount++;
                }
            }
        }

        public sealed class MultipleValue : QueryCaptureTests
        {
            [Fact]
            public void ShouldConvertAllTheValues()
            {
                var parameters = new Dictionary<string, object>();
                this.AddLookupItem("key", "2", "3");

                var capture = QueryCapture.Create("key", typeof(int[]), "", this.converters.TryGetValue);
                capture.ParseParameters(this.lookup, parameters);

                parameters[CapturedParameter].Should().BeAssignableTo<int[]>()
                    .Which.Should().BeEquivalentTo(2, 3);
            }
        }

        public sealed class SingleValue : QueryCaptureTests
        {
            [Fact]
            public void ShouldConvertTheValue()
            {
                var parameters = new Dictionary<string, object>();
                this.AddLookupItem("key", "2");

                var capture = QueryCapture.Create("key", typeof(int), "", this.converters.TryGetValue);
                capture.ParseParameters(this.lookup, parameters);

                parameters[CapturedParameter].Should().Be(2);
            }

            [Fact]
            public void ShouldReturnTheFirstSuccessfullyConvertedValue()
            {
                var parameters = new Dictionary<string, object>();
                this.AddLookupItem("key", "not an integer", "3");

                var capture = QueryCapture.Create("key", typeof(int), "", this.converters.TryGetValue);
                capture.ParseParameters(this.lookup, parameters);

                parameters[CapturedParameter].Should().Be(3);
            }
        }

        private sealed class FakeConverter : IQueryValueConverter
        {
            public string ParameterName => CapturedParameter;

            public bool TryConvertValue(StringSegment value, out object result)
            {
                if (int.TryParse(value.ToString(), out int converted))
                {
                    result = converted;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }
    }
}
