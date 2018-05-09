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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteCodeFix))]
    [Shared]
    public sealed class RouteCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                RouteAnalyzer.UnescapedBraceId,
                RouteAnalyzer.UnknownParameterId);

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
                else if (diagnostic.Id == RouteAnalyzer.UnknownParameterId)
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
