namespace Crest.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Routing;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    internal sealed class UrlValidator : UrlParser
    {
        private readonly SyntaxNodeAnalysisContext context;
        private readonly IReadOnlyList<ParameterData> parameters;
        private readonly IReadOnlyDictionary<string, ParameterSyntax> parameterSyntax;
        private SyntaxToken currentNode;

        public UrlValidator(
            bool canReadBody,
            SyntaxNodeAnalysisContext context,
            IEnumerable<ParameterSyntax> parameters)
            : base(canReadBody)
        {
            this.context = context;
            this.parameterSyntax = parameters.ToDictionary(ps => ps.Identifier.Text);
            this.parameters =
                this.parameterSyntax.Values
                    .Select(this.ConvertParameter)
                    .ToList();
        }

        public void Analyze(AttributeSyntax attribute)
        {
            string url = this.GetRouteUrl(attribute);
            if (url != string.Empty)
            {
                this.ParseUrl(url, this.parameters);
            }
        }

        protected override void OnCaptureBody(Type parameterType, string name)
        {
        }

        protected override void OnCaptureSegment(Type parameterType, string name)
        {
        }

        protected override void OnError(ErrorType error, string parameter)
        {
            if (this.parameterSyntax.TryGetValue(parameter, out ParameterSyntax syntax))
            {
                this.RaiseError(error, syntax.GetLocation());
            }
        }

        protected override void OnError(ErrorType error, int start, int length, string value)
        {
            string rawText = this.currentNode.Text ?? string.Empty;
            int quotesLength = rawText.StartsWith("@", StringComparison.Ordinal) ? 2 : 1;

            var span = new TextSpan(
                this.currentNode.Span.Start + quotesLength + start,
                length);

            var location = Location.Create(
                this.currentNode.SyntaxTree,
                span);

            this.RaiseError(error, location);
        }

        protected override void OnLiteralSegment(string value)
        {
        }

        protected override void OnQueryCatchAll(string name)
        {
        }

        protected override void OnQueryParameter(string key, Type parameterType, string name)
        {
        }

        private static DiagnosticDescriptor GetDiagnostic(ErrorType error)
        {
            switch (error)
            {
                case ErrorType.CannotBeMarkedAsFromBody:
                    return RouteAnalyzer.CannotBeMarkedAsFromBodyRule;

                case ErrorType.DuplicateParameter:
                    return RouteAnalyzer.DuplicateCaptureRule;

                case ErrorType.IncorrectCatchAllType:
                    return RouteAnalyzer.IncorrectCatchAllTypeRule;

                case ErrorType.MissingClosingBrace:
                    return RouteAnalyzer.MissingClosingBraceRule;

                case ErrorType.MissingQueryValue:
                    return RouteAnalyzer.MissingQueryValueRule;

                case ErrorType.MultipleBodyParameters:
                    return RouteAnalyzer.MultipleBodyParametersRule;

                case ErrorType.MustBeOptional:
                    return RouteAnalyzer.MustBeOptionalRule;

                case ErrorType.MustCaptureQueryValue:
                    return RouteAnalyzer.MustCaptureQueryValueRule;

                case ErrorType.ParameterNotFound:
                    return RouteAnalyzer.ParameterNotFoundRule;

                case ErrorType.UnescapedBrace:
                    return RouteAnalyzer.UnescapedBraceRule;

                case ErrorType.UnknownParameter:
                    return RouteAnalyzer.UnknownParameterRule;

                default:
                    return null;
            }
        }

        private ParameterData ConvertParameter(ParameterSyntax syntax)
        {
            // In order to pass the validation for the catch-all query
            // parameters, we need to know if a parameter is an object or not.
            // We also need to know if the parameter is a value type or
            // reference, as if it's a value type it must be optional if it is
            // used as a query capture.
            Type type = typeof(string);
            if (this.context.SemanticModel.GetDeclaredSymbol(syntax) is IParameterSymbol symbol)
            {
                if ((symbol.Type.SpecialType == SpecialType.System_Object) ||
                    (symbol.Type.TypeKind == TypeKind.Dynamic))
                {
                    type = typeof(object);
                }
                else if (symbol.Type.IsValueType)
                {
                    type = typeof(int);
                }
            }

            return new ParameterData
            {
                HasBodyAttribute = RouteAttributeInfo.GetFromBody(syntax) != null,
                IsOptional = syntax.Default != null,
                Name = syntax.Identifier.Text,
                ParameterType = type
            };
        }

        private string GetRouteUrl(AttributeSyntax attribute)
        {
            AttributeArgumentSyntax argument = attribute.ArgumentList.Arguments.FirstOrDefault();
            if (argument?.Expression is LiteralExpressionSyntax literal)
            {
                this.currentNode = literal.Token;
                return this.currentNode.ValueText;
            }
            else
            {
                return string.Empty;
            }
        }

        private void RaiseError(ErrorType error, Location location)
        {
            DiagnosticDescriptor diagnostic = GetDiagnostic(error);
            if (diagnostic != null)
            {
                this.context.ReportDiagnostic(Diagnostic.Create(diagnostic, location));
            }
        }
    }
}
