namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RouteAnalyzer : DiagnosticAnalyzer
    {
        public const string DuplicateCaptureId = "MissingVersionAttribute";

        internal static readonly DiagnosticDescriptor DuplicateCaptureRule =
            new DiagnosticDescriptor(
                DuplicateCaptureId,
                "Duplicate parameter capture",
                "Parameter is captured multiple times",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Parameters may only be captured once in the URL.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DuplicateCaptureRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var validator = new UrlValidator(context, method.ParameterList.Parameters);

            foreach (AttributeSyntax attribute in RouteAttributeInfo.GetRouteAttributes(method))
            {
                validator.Analyze(attribute);
            }
        }
    }
}
