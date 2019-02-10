namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Allows the analyzing of route URLs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RouteAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Captured parameter cannot be marked as FromBody.
        /// </summary>
        public const string CannotBeMarkedAsFromBodyId = "CannotBeMarkedAsFromBody";

        /// <summary>
        /// Parameter is captured multiple times.
        /// </summary>
        public const string DuplicateCaptureId = "DuplicateCapture";

        /// <summary>
        /// Catch-all must be declared as object or dynamic.
        /// </summary>
        public const string IncorrectCatchAllTypeId = "IncorrectCatchAllType";

        /// <summary>
        /// Missing closing brace.
        /// </summary>
        public const string MissingClosingBraceId = "MissingClosingBrace";

        /// <summary>
        /// Multiple FromBody parameters are not allowed.
        /// </summary>
        public const string MultipleBodyParametersId = "MultipleBodyParameters";

        /// <summary>
        /// Multiple query capture remaining values parameters are not allowed.
        /// </summary>
        public const string MultipleCatchAllParametersId = "MultipleCatchAllParameters";

        /// <summary>
        /// Query parameters must be marked as optional.
        /// </summary>
        public const string MustBeOptionalId = "MustBeOptional";

        /// <summary>
        /// Parameter must be captured in the route URL.
        /// </summary>
        public const string ParameterNotFoundId = "ParameterNotFound";

        /// <summary>
        /// No matching method parameter.
        /// </summary>
        public const string UnknownParameterId = "UnknownParameter";

        internal static readonly DiagnosticDescriptor CannotBeMarkedAsFromBodyRule =
            new DiagnosticDescriptor(
                CannotBeMarkedAsFromBodyId,
                "Parameter marked as FromBody",
                "Captured parameter cannot be marked as FromBody",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Parameter is captured in the URL so cannot come from the request body.");

        internal static readonly DiagnosticDescriptor DuplicateCaptureRule =
            new DiagnosticDescriptor(
                DuplicateCaptureId,
                "Duplicate parameter capture",
                "Parameter is captured multiple times",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Parameters may only be captured once in the URL.");

        internal static readonly DiagnosticDescriptor IncorrectCatchAllTypeRule =
            new DiagnosticDescriptor(
                IncorrectCatchAllTypeId,
                "Incorrect catch-all type",
                "Catch-all must be declared as object or dynamic",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Query catch-all parameter must be an object or a dynamic object.");

        internal static readonly DiagnosticDescriptor MissingClosingBraceRule =
            new DiagnosticDescriptor(
                MissingClosingBraceId,
                "Missing closing brace",
                "Missing closing brace",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Open braces must be matched with a closing brace.");

        internal static readonly DiagnosticDescriptor MultipleBodyParametersRule =
            new DiagnosticDescriptor(
                MultipleBodyParametersId,
                "Multiple FromBody parameters",
                "Multiple FromBody parameters are not allowed",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Only one parameter can be marked as coming from the request body.");

        internal static readonly DiagnosticDescriptor MultipleCatchAllParametersRule =
            new DiagnosticDescriptor(
                MultipleCatchAllParametersId,
                "Multiple catch all parameters",
                "Multiple query capture remaining values parameters are not allowed",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Only one parameter can be used to capture the remaining query key/values.");

        internal static readonly DiagnosticDescriptor MustBeOptionalRule =
            new DiagnosticDescriptor(
                MustBeOptionalId,
                "Parameter must be optional",
                "Query parameters must be marked as optional",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Query parameters in the route are optional, therefore, the parameter must me optional.");

        internal static readonly DiagnosticDescriptor ParameterNotFoundRule =
            new DiagnosticDescriptor(
                ParameterNotFoundId,
                "Parameter not found",
                "Parameter must be captured in the route URL",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Method parameters must be specified as captures in the URL.");

        internal static readonly DiagnosticDescriptor UnknownParameterRule =
            new DiagnosticDescriptor(
                UnknownParameterId,
                "Unknown parameter",
                "No matching method parameter",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "The method contains no parameter matching the capture name.");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                CannotBeMarkedAsFromBodyRule,
                DuplicateCaptureRule,
                IncorrectCatchAllTypeRule,
                MissingClosingBraceRule,
                MultipleBodyParametersRule,
                MultipleCatchAllParametersRule,
                MustBeOptionalRule,
                ParameterNotFoundRule,
                UnknownParameterRule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            UrlValidator validator = null;

            foreach (AttributeSyntax attribute in RouteAttributeInfo.GetRouteAttributes(method))
            {
                if (validator == null)
                {
                    validator = new UrlValidator(
                        RouteAttributeInfo.VerbUsesBody(attribute),
                        context,
                        method.ParameterList.Parameters);
                }

                validator.Analyze(attribute);
            }
        }
    }
}
