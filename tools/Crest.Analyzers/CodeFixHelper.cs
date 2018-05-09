namespace Crest.Analyzers
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    internal static class CodeFixHelper
    {
        internal static MethodDeclarationSyntax GetRouteMethod(SyntaxNode root, TextSpan span)
        {
            return root.FindToken(span.Start)
                       .Parent
                       .AncestorsAndSelf()
                       .OfType<MethodDeclarationSyntax>()
                       .FirstOrDefault();
        }
    }
}
