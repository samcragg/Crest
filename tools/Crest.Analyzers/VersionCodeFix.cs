namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VersionCodeFix))]
    [Shared]
    public sealed class VersionCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(VersionAnalyzer.MissingVersionAttributeId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First(d => d.Id == VersionAnalyzer.MissingVersionAttributeId);

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add attribute",
                    c => this.AddAttribute(context, c),
                    VersionAnalyzer.MissingVersionAttributeId),
                diagnostic);

            return Task.FromResult(0);
        }

        private async Task<Document> AddAttribute(CodeFixContext context, CancellationToken token)
        {
            Document document = context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            MethodDeclarationSyntax method = CodeFixHelper.GetRouteMethod(root, context.Span);
            if (method == null)
            {
                return document;
            }

            AttributeSyntax newAttribute =
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("Version"),
                    SyntaxFactory.AttributeArgumentList().WithArguments(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))))));

            AttributeListSyntax attributeList =
                SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(newAttribute));

            SyntaxList<AttributeListSyntax> attributes = method.AttributeLists;
            attributes = attributes.Add(attributeList);

            MethodDeclarationSyntax newMethod = method.WithAttributeLists(attributes);
            return document.WithSyntaxRoot(root.ReplaceNode(method, newMethod));
        }
    }
}
