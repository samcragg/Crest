namespace Analyzers.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using NUnit.Framework;

    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract class DiagnosticVerifier<TProvider>
        where TProvider : DiagnosticAnalyzer, new()
    {
        internal const string CSharpDefaultFileExt = ".cs";
        internal const string DefaultFilePathPrefix = "Test";
        internal const string TestProjectName = "TestProject";
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source)
        {
            return CreateProject(new[] { source }).Documents.First();
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static Diagnostic[] GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilationWithAnalyzers = project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
        protected void VerifyDiagnostic(string source, params DiagnosticResult[] expected)
        {
            VerifyDiagnostics(new[] { source }, expected);
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        protected void VerifyDiagnostics(string[] sources, params DiagnosticResult[] expected)
        {
            var analyzer = new TProvider();
            Diagnostic[] diagnostics = GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources));
            VerifyDiagnosticResults(diagnostics, analyzer, expected);
        }

        private static Project CreateProject(string[] sources)
        {
            string fileNamePrefix = DefaultFilePathPrefix;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            for (int i = 0; i < sources.Length; i++)
            {
                string newFileName = fileNamePrefix + i + CSharpDefaultFileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(sources[i]));
            }

            return solution.GetProject(projectId);
        }

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.IsTrue(location.IsInSource,
                                $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                            string resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }
            return builder.ToString();
        }

        private static Document[] GetDocuments(string[] sources)
        {
            var project = CreateProject(sources);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new SystemException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        private static void ThrowAssertionException(string message, DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder(message);
            builder.AppendLine().AppendLine().AppendLine("Diagnostics:");

            if (diagnostics.Any())
            {
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    builder.Append("    // ").AppendLine(diagnostic.ToString());

                    DiagnosticDescriptor rule = analyzer.SupportedDiagnostics.FirstOrDefault(r => r?.Id == diagnostic.Id);
                    if (rule != null)
                    {
                        Location location = diagnostic.Location;
                        if (location == Location.None)
                        {
                            builder.Append("    GetGlobalResult(").Append(analyzer.GetType().Name).Append('.').Append(rule.Id);
                        }
                        else
                        {
                            LinePosition linePosition = location.GetLineSpan().StartLinePosition;
                            builder.AppendFormat(
                                "    GetCSharpResultAt({0}, {1}, {2}.{3})",
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzer.GetType().Name,
                                rule.Id);
                        }

                        builder.AppendLine().AppendLine();
                    }
                }
            }
            else
            {
                builder.AppendLine("    NONE");
            }

            throw new AssertionException(builder.ToString());
        }

        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualLinePosition = actual.GetLineSpan().StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if ((actualLinePosition.Line > 0) && ((actualLinePosition.Line + 1) != expected.Line))
            {
                ThrowAssertionException(
                    $"Expected diagnostic to be on line {expected.Line} but was actually on line {actualLinePosition.Line + 1}",
                    analyzer,
                    diagnostic);
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if ((actualLinePosition.Character > 0) && ((actualLinePosition.Character + 1) != expected.Column))
            {
                ThrowAssertionException(
                    $"Expected diagnostic to start at column {expected.Column} but was actually at column {actualLinePosition.Character + 1}",
                    analyzer,
                    diagnostic);
            }
        }

        private static void VerifyDiagnosticResults(Diagnostic[] actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            if (expectedResults.Length != actualResults.Length)
            {
                ThrowAssertionException(
                    $"Mismatch between number of diagnostics returned, expected {expectedResults.Length} but got {actualResults.Length}",
                    analyzer,
                    actualResults);
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.IsTrue(false,
                            string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
                            FormatDiagnostics(analyzer, actual)));
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        Assert.IsTrue(false,
                            string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                expected.Locations.Length - 1, additionalLocations.Length,
                                FormatDiagnostics(analyzer, actual)));
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                Assert.AreEqual(
                    expected.Id,
                    actual.Id,
                    "\n\nDiagnostic:\n\n" + FormatDiagnostics(analyzer, actual));

                Assert.AreEqual(
                    expected.Severity,
                    actual.Severity,
                    "\n\nDiagnostic:\n\n" + FormatDiagnostics(analyzer, actual));

                if (!string.IsNullOrEmpty(expected.Message))
                {
                    Assert.AreEqual(
                        expected.Message,
                        actual.GetMessage(),
                        "\n\nDiagnostic:\n\n" + FormatDiagnostics(analyzer, actual));
                }
            }
        }
    }
}
