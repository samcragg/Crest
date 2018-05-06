namespace Analyzers.UnitTests
{
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public sealed class RouteAnalyzerTests : DiagnosticVerifier<RouteAnalyzer>
    {
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
    Task Method(int capture);
}";

            var expected = new DiagnosticResult
            {
                Id = RouteAnalyzer.MissingClosingBraceId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 4, column: 19) }
            };

            VerifyDiagnostic(Source, expected);
        }
    }
}
