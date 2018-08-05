namespace Host.UnitTests.Diagnostics
{
    using System.Linq;
    using System.Xml.Linq;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class HtmlReporterTests
    {
        private readonly HtmlReporter reporter = new HtmlReporter();

        private static string FindRowValue(XElement root, string label)
        {
            XElement row = root.Descendants("tr")
                               .Where(r => r.Element("th").Value == label)
                               .Single();

            return row.Element("td").Value;
        }

        private XElement GetHtml()
        {
            string html = this.reporter.GenerateReport();
            return XDocument.Parse("<root>" + html + "</root>").Root;
        }

        public sealed class Write_Counter : HtmlReporterTests
        {
            [Fact]
            public void ShouldIncludeTheUnit()
            {
                IUnit unit = new LabelUnit("unit");

                this.reporter.Write("counter", new Counter(), unit);
                string result = FindRowValue(this.GetHtml(), "Value");

                result.Should().EndWith("unit");
            }

            [Fact]
            public void ShouldIncludeTheValue()
            {
                var counter = new Counter();
                counter.Increment();

                this.reporter.Write("counter", counter, null);
                string result = FindRowValue(this.GetHtml(), "Value");

                result.Should().Be("1");
            }

            [Fact]
            public void ShouldTitleizeTheLabel()
            {
                this.reporter.Write("myLabel", new Counter(), null);
                XElement result = this.GetHtml();

                result.Element("h3").Value.Should().Be("My Label");
            }
        }

        public sealed class Write_Gauage : HtmlReporterTests
        {
            private readonly Gauge gauge;

            public Write_Gauage()
            {
                this.gauge = new Gauge(Substitute.For<ITimeProvider>());
            }

            [Fact]
            public void ShouldIncludeTheAverage()
            {
                this.gauge.Add(50);
                this.gauge.Add(75);
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                string result = FindRowValue(this.GetHtml(), "Mean");

                result.Should().Be("75");
            }

            [Fact]
            public void ShouldIncludeTheBasicStats()
            {
                this.gauge.Add(50);
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                XElement result = this.GetHtml();

                FindRowValue(result, "Min").Should().Be("50");
                FindRowValue(result, "Max").Should().Be("100");
            }

            [Fact]
            public void ShouldIncludeTheMovingAverages()
            {
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                XElement result = this.GetHtml();

                FindRowValue(result, "1 Min Average").Should().Be("100");
                FindRowValue(result, "5 Min Average").Should().Be("100");
                FindRowValue(result, "15 Min Average").Should().Be("100");
            }

            [Fact]
            public void ShouldIncludeTheUnit()
            {
                IUnit unit = new LabelUnit("unit");
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, unit);
                string result = FindRowValue(this.GetHtml(), "Mean");

                result.Should().EndWith("unit");
            }

            [Fact]
            public void ShouldTitleizeTheLabel()
            {
                this.reporter.Write("myLabel", this.gauge, null);
                XElement result = this.GetHtml();

                result.Element("h3").Value.Should().Be("My Label");
            }
        }
    }
}
