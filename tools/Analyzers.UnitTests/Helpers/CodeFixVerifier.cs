namespace Analyzers.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using Xunit.Sdk;

    /// <summary>
    /// Superclass of all unit tests made for diagnostics with code fixes.
    /// </summary>
    public abstract class CodeFixVerifier<TDiagnostic, TCodeFix> : DiagnosticVerifier<TDiagnostic>
        where TDiagnostic : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        /// <summary>
        /// Called to test a C# code fix when applied on the inputted string as a source.
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which code fix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A boolean controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected async Task VerifyFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            DiagnosticAnalyzer analyzer = new TDiagnostic();
            CodeFixProvider codeFixProvider = new TCodeFix();
            Document document = CreateDocument(oldSource);
            Diagnostic[] analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
            Diagnostic[] compilerDiagnostics = await GetCompilerDiagnostics(document);

            for (int i = 0; i < analyzerDiagnostics.Length; i++)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context);

                if (!actions.Any())
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFix(document, actions[codeFixIndex.Value]);
                    break;
                }

                document = await ApplyFix(document, actions[0]);
                analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });

                IEnumerable<Diagnostic> newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync(), Formatter.Annotation, document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document));

                    throw new XunitException(
                        string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                            string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                            document.GetSyntaxRootAsync().Result.ToFullString()));
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any())
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            string actual = await GetStringFromDocument(document);
            NormalizeLineEndings(newSource).Should().Be(NormalizeLineEndings(actual));
        }

        private static async Task<Document> ApplyFix(Document document, CodeAction codeAction)
        {
            ImmutableArray<CodeActionOperation> operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            Solution solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }

        private static async Task<Diagnostic[]> GetCompilerDiagnostics(Document document)
        {
            SemanticModel model = await document.GetSemanticModelAsync();
            return model.GetDiagnostics().ToArray();
        }

        private static IEnumerable<Diagnostic> GetNewDiagnostics(Diagnostic[] diagnostics, Diagnostic[] newDiagnostics)
        {
            Array.Sort(diagnostics, (a, b) => a.Location.SourceSpan.Start.CompareTo(b.Location.SourceSpan.Start));
            Array.Sort(newDiagnostics, (a, b) => a.Location.SourceSpan.Start.CompareTo(b.Location.SourceSpan.Start));

            int oldIndex = 0;
            int newIndex = 0;

            while (newIndex < newDiagnostics.Length)
            {
                if (oldIndex < diagnostics.Length && diagnostics[oldIndex].Id == newDiagnostics[newIndex].Id)
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newDiagnostics[newIndex++];
                }
            }
        }

        private static async Task<string> GetStringFromDocument(Document document)
        {
            Document simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation);
            SyntaxNode root = await simplifiedDoc.GetSyntaxRootAsync();
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        private static string NormalizeLineEndings(string value)
        {
            return value.Replace("\r\n", "\n");
        }
    }
}
