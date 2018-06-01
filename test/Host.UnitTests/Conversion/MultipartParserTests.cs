namespace Host.UnitTests.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class MultipartParserTests
    {
        private const string Boundary = "BoundaryText";
        private const string NewLine = "\r\n";
        private readonly MultipartParser parser;
        private readonly MemoryStream stream;

        private MultipartParserTests()
        {
            this.stream = new MemoryStream();
            this.parser = new MultipartParser(Boundary, this.stream);
        }

        protected private string GetPartBody(MultipartParser.BodyPart part)
        {
            return this.GetPartText(part.BodyStart, part.BodyEnd);
        }

        protected private string GetPartHeader(MultipartParser.BodyPart part)
        {
            return this.GetPartText(part.HeaderStart, part.HeaderEnd);
        }

        protected void SetStream(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        private string GetPartText(int start, int end)
        {
            long position = this.stream.Position;
            this.stream.Position = start;

            byte[] buffer = new byte[end - start];
            this.stream.Read(buffer, 0, buffer.Length);
            this.stream.Position = position;

            return Encoding.ASCII.GetString(buffer);
        }

        public sealed class Parse : MultipartParserTests
        {
            [Fact]
            public void ShouldIgnoreEpilogue()
            {
                this.SetStream(
                    "--" + Boundary + NewLine +
                    NewLine +
                    "Body part" + NewLine +
                    "--" + Boundary + "--" + NewLine +
                    NewLine +
                    "This is the epilogue.  It is also to be ignored.");

                IEnumerable<MultipartParser.BodyPart> result = this.parser.Parse();
                string text = this.GetPartBody(result.Single());

                text.Should().Be("Body part");
            }

            [Fact]
            public void ShouldIgnorePreamble()
            {
                this.SetStream(
                    "This is the preamble.  It is to be ignored, though it" + NewLine +
                    "is a handy place for composition agents to include an" + NewLine +
                    "explanatory note to non - MIME conformant readers." + NewLine +
                    NewLine +
                    "--" + Boundary + NewLine +
                    NewLine +
                    "Body part" + NewLine +
                    "--" + Boundary + "--");

                IEnumerable<MultipartParser.BodyPart> result = this.parser.Parse();
                string text = this.GetPartBody(result.Single());

                text.Should().Be("Body part");
            }

            [Fact]
            public void ShouldIgnoreTransportPadding()
            {
                this.SetStream(
                    "--" + Boundary + "        " + NewLine +
                    NewLine +
                    "Body part" + NewLine +
                    "--" + Boundary + "--");

                IEnumerable<MultipartParser.BodyPart> result = this.parser.Parse();
                string text = this.GetPartBody(result.Single());

                text.Should().Be("Body part");
            }

            [Fact]
            public void ShouldMatchTheWholeBoundary()
            {
                string bodyText = "--" + Boundary.Substring(0, Boundary.Length - 1);
                this.SetStream(
                    "--" + Boundary + NewLine +
                    NewLine +
                    bodyText + NewLine +
                    "--" + Boundary + "--");

                IEnumerable<MultipartParser.BodyPart> result = this.parser.Parse();
                string text = this.GetPartBody(result.Single());

                text.Should().Be(bodyText);
            }

            [Fact]
            public void ShouldReadAllTheParts()
            {
                this.SetStream(
                    "--" + Boundary + NewLine +
                    NewLine +
                    "Part 1" + NewLine +
                    "--" + Boundary + NewLine +
                    NewLine +
                    "Part 2" + NewLine +
                    "--" + Boundary + "--");

                string[] results =
                    this.parser.Parse().Select(this.GetPartBody).ToArray();

                results.Should().HaveCount(2);
                results.Should().HaveElementAt(0, "Part 1");
                results.Should().HaveElementAt(1, "Part 2");
            }

            [Fact]
            public void ShouldReadTheBodyWithTrailingLineBreak()
            {
                this.SetStream(
                    "--" + Boundary + NewLine +
                    NewLine +
                    "Ends with a linebreak." + NewLine +
                    NewLine +
                    "--" + Boundary + "--");

                IEnumerable<MultipartParser.BodyPart> result = this.parser.Parse();
                string text = this.GetPartBody(result.Single());

                text.Should().Be("Ends with a linebreak." + NewLine);
            }

            [Fact]
            public void ShouldReadThePartHeader()
            {
                const string Header = "Content-type: text/plain" + NewLine;

                this.SetStream(
                    "--" + Boundary + NewLine +
                    Header +
                    NewLine +
                    "Part1" + NewLine +
                    "--" + Boundary + NewLine +
                    Header +
                    NewLine +
                    "Part2" + NewLine +
                    "--" + Boundary + "--");

                List<string> headers = this.parser.Parse().Select(this.GetPartHeader).ToList();

                headers.Should().HaveCount(2);
                headers.Should().OnlyContain(x => string.Equals(x, Header));
            }

            [Fact]
            public void ShouldThrowIfTheBoundaryIsNotFound()
            {
                this.SetStream(Boundary);

                Action action = () => this.parser.Parse().ToList();

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldThrowIfTheCloseDelimiterIsNotFound()
            {
                this.SetStream(
                    "--" + Boundary + NewLine +
                    NewLine +
                    "--" + Boundary + NewLine);

                Action action = () => this.parser.Parse().ToList();

                action.Should().Throw<FormatException>();
            }
        }
    }
}
