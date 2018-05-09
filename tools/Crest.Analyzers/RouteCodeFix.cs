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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteCodeFix))]
    [Shared]
    public sealed class RouteCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(RouteAnalyzer.UnescapedBraceId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == RouteAnalyzer.UnescapedBraceId)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Escape brace",
                            c => this.EscapeBrace(context, c),
                            RouteAnalyzer.UnescapedBraceId),
                        diagnostic);
                }
            }

            return Task.FromResult(0);
        }

        private async Task<Document> EscapeBrace(CodeFixContext context, CancellationToken token)
        {
            Document document = context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            SyntaxToken currentToken = root.FindToken(context.Span.Start);

            int offset = context.Span.Start - currentToken.Span.Start;
            string brace = currentToken.Text[offset] == '{' ? "{" : "}";
            SyntaxToken newToken = SyntaxFactory.Literal(
                currentToken.Text.Insert(offset, brace), null);

            return document.WithSyntaxRoot(root.ReplaceToken(currentToken, newToken));
        }
    }
}
