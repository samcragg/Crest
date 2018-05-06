﻿namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RouteAnalyzer : DiagnosticAnalyzer
    {
        public const string DuplicateCaptureId = "DuplicateCapture";
        public const string MissingClosingBraceId = "MissingClosingBrace";
        public const string MissingQueryValueId = "MissingQueryValue";
        public const string MustBeOptionalId = "MustBeOptional";

        internal static readonly DiagnosticDescriptor DuplicateCaptureRule =
            new DiagnosticDescriptor(
                DuplicateCaptureId,
                "Duplicate parameter capture",
                "Parameter is captured multiple times",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Parameters may only be captured once in the URL.");

        internal static readonly DiagnosticDescriptor MissingClosingBraceRule =
            new DiagnosticDescriptor(
                MissingClosingBraceId,
                "Missing closing brace",
                "Missing closing brace",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Open braces must be matched with a closing brace.");

        internal static readonly DiagnosticDescriptor MissingQueryValueRule =
            new DiagnosticDescriptor(
                MissingQueryValueId,
                "Missing query value",
                "Query parameters should be in the form 'key={value}'",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Query parameters must consist of a key and capture value.");

        internal static readonly DiagnosticDescriptor MustBeOptionalRule =
            new DiagnosticDescriptor(
                MustBeOptionalId,
                "Parameter must be optional",
                "Query parameters must be marked as optional",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Query parameters in the route are optional, therefore, the parameter must me optional.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DuplicateCaptureRule,
                MissingClosingBraceRule,
                MissingQueryValueRule,
                MustBeOptionalRule);

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
