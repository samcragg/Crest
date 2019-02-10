namespace Host.UnitTests.Routing.Captures
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class QueryCaptureTests
    {
        private const string CapturedParameter = "parameter";
        private readonly Dictionary<Type, Func<string, IQueryValueConverter>> converters;
        private readonly ILookup<string, string> lookup = Substitute.For<ILookup<string, string>>();
        private readonly IQueryValueConverter converter = new FakeConverter();

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

        public sealed class CreateCatchAll : QueryCaptureTests
        {
            [Fact]
            public void ShouldReturnAClassThatCapturesTheParameter()
            {
                var parameters = new Dictionary<string, object>();
                ILookup<string, string> lookup = Substitute.For<ILookup<string, string>>();

                var result = QueryCapture.CreateCatchAll("name");
                result.ParseParameters(lookup, parameters);

                parameters.Should().ContainKey("name")
                          .WhichValue.Should().BeAssignableTo<DynamicObject>();
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

            [Fact]
            public void ShouldSkipInvalidValues()
            {
                var parameters = new Dictionary<string, object>();
                this.AddLookupItem("key", "invalid", "3");

                var capture = QueryCapture.Create("key", typeof(int[]), "", this.converters.TryGetValue);
                capture.ParseParameters(this.lookup, parameters);

                ((int[])parameters[CapturedParameter]).Should().BeEquivalentTo(3);
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

            public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
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
