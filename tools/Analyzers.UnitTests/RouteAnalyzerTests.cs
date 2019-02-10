namespace Analyzers.UnitTests
{
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public sealed class RouteAnalyzerTests : DiagnosticVerifier<RouteAnalyzer>
    {
        [Theory]
        [InlineData("dynamic")]
        [InlineData("object")]
        [InlineData("System.Object")]
        public void ShouldAllowCatchAllQueryParameters(string type)
        {
            string source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route{?parameter*}"")]
    Task Method(" + type + @" parameter);
}";

            this.VerifyDiagnostic(source);
        }

        [Fact]
        public void ShouldAllowExplicitBodyParameters()
        {
            const string Source = Code.Usings + Code.FromBodyAttribute + Code.PutAttribute + @"
interface IRoute
{
    [Put(""/{parameter}"")]
    Task Method(string parameter, [FromBody]string requestBody);
}";

            this.VerifyDiagnostic(Source);
        }

        [Fact]
        public void ShouldAllowImplicitBodyParameters()
        {
            const string Source = Code.Usings + Code.PutAttribute + @"
interface IRoute
{
    [Put(""/"")]
    Task Method(string requestBody);
}";

            this.VerifyDiagnostic(Source);
        }

        [Fact]
        public void ShouldCheckCatchAllType()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route{?parameter*}"")]
    Task Method(string parameter);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.IncorrectCatchAllTypeId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 17) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForCapturesMarkedAsFromBody()
        {
            const string Source = Code.Usings + Code.FromBodyAttribute + Code.GetAttribute + @"
            interface IRoute
            {
                [Get(""/{parameter}"")]
                Task Method([FromBody]string parameter);
            }";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.CannotBeMarkedAsFromBodyId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 29) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForDuplicateCaptures()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/{capture}/{capture}"")]
    Task Method(int capture);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.DuplicateCaptureId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 17) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForMissingClosingBraces()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/{capture"")]
    Task Method();
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MissingClosingBraceId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 12) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForMultipleFromBodyParameters()
        {
            const string Source = Code.Usings + Code.FromBodyAttribute + Code.PutAttribute + @"
            interface IRoute
            {
                [Put(""/"")]
                Task Method([FromBody]string p1, [FromBody]string p2);
            }";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MultipleBodyParametersId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 50) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForMultipleCatchAllParameters()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
            interface IRoute
            {
                [Get(""/query{?one*,two*}"")]
                Task Method(object one, object two);
            }";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MultipleCatchAllParametersId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 41) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForUnknownParameters()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/{unknown}"")]
    Task Method();
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.UnknownParameterId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 13) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckParametersAreCaptured()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route"")]
    Task Method(int id);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.ParameterNotFoundId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 17) }
            };

            this.VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckThatQueryCapturesAreOptional()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route{?capture}"")]
    Task Method(int capture);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MustBeOptionalId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 17) }
            };

            this.VerifyDiagnostic(Source, expected);
        }
    }
}
