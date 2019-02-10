namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Provides code fixes for errors in the route URL.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteCodeFix))]
    [Shared]
    public sealed class RouteCodeFix : CodeFixProvider
    {
        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                RouteAnalyzer.UnknownParameterId);

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <inheritdoc />
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == RouteAnalyzer.UnknownParameterId)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add parameter",
                            c => this.AddParameter(context, c),
                            RouteAnalyzer.UnknownParameterId),
                        diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private async Task<Document> AddParameter(CodeFixContext context, CancellationToken token)
        {
            Document document = context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            MethodDeclarationSyntax method = CodeFixHelper.GetRouteMethod(root, context.Span);
            if (method == null)
            {
                return document;
            }

            SyntaxToken captureToken = root.FindToken(context.Span.Start);
            int offset = context.Span.Start - captureToken.SpanStart;
            string parameterName = captureToken.Text.Substring(offset, context.Span.Length);
            ParameterSyntax newParameter =
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName("string"));

            MethodDeclarationSyntax newMethod = method.WithParameterList(
                method.ParameterList.AddParameters(newParameter));

            return document.WithSyntaxRoot(root.ReplaceNode(method, newMethod));
        }
    }
}
