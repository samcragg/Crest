namespace Analyzers.UnitTests
{
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public sealed class RouteAnalyzerTests : DiagnosticVerifier<RouteAnalyzer>
    {
        [Fact]
        public void ShouldAllowExplicitBodyParameters()
        {
            const string Source = Code.Usings + Code.FromBodyAttribute + Code.PutAttribute + @"
interface IRoute
{
    [Put(""/{parameter}"")]
    Task Method(string parameter, [FromBody]string requestBody);
}";

            VerifyDiagnostic(Source);
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

            VerifyDiagnostic(Source);
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

            VerifyDiagnostic(Source, expected);
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

            VerifyDiagnostic(Source, expected);
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
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 19) }
            };

            VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForMissingQueryValue()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route?queryKey"")]
    Task Method();
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MissingQueryValueId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 18) }
            };

            VerifyDiagnostic(Source, expected);
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

            VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckForUnescapedBraces()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route{unescaped"")]
    Task Method();
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.UnescapedBraceId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 17) }
            };

            VerifyDiagnostic(Source, expected);
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

            VerifyDiagnostic(Source, expected);
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

            VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckThatQueryCapturesAreOptional()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route?key={capture}"")]
    Task Method(int capture);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MustBeOptionalId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 17) }
            };

            VerifyDiagnostic(Source, expected);
        }

        [Fact]
        public void ShouldCheckThatQueryValuesAreCaptured()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route?key=value"")]
    Task Method();
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MustCaptureQueryValueId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 22) }
            };

            VerifyDiagnostic(Source, expected);
        }
    }
}
