namespace Analyzers.UnitTests
{
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;

    [TestFixture]
    public sealed class VersionAnalyzerTests : DiagnosticVerifier<VersionAnalyzer>
    {
        [Test]
        public void ShouldCheckTheVersionAttributeIsSpecified()
        {
            const string Source = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""/route"")]
    Task Method();
}";

            // We expect the identifier to be highlighted (i.e. Method in the above)
            var expected = new DiagnosticResult
            {
                Id = VersionAnalyzer.MissingVersionAttributeId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 10) }
            };

            VerifyDiagnostic(Source, expected);
        }

        [Test]
        public void ShouldCheckTheVersionRange()
        {
            const string Source = Code.Usings + Code.GetAttribute + Code.VersionAttribute + @"
interface IRoute
{
    [Get(""/route"")]
    [Version(3,1)]
    Task Method();
}";

            // We expect the parameters (including parentheses) of the attribute to be highlighted
            var expected = new DiagnosticResult
            {
                Id = VersionAnalyzer.VersionOutOfRangeId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(line: 5, column: 13) }
            };

            VerifyDiagnostic(Source, expected);
        }
    }
}
