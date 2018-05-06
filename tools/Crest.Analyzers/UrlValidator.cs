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
        private readonly ISet<string> optionalParameters = new SortedSet<string>();
        private readonly IReadOnlyDictionary<string, Type> parameterNames;
        private readonly IReadOnlyDictionary<string, ParameterSyntax> parameterSyntax;
        private SyntaxToken currentNode;

        public UrlValidator(SyntaxNodeAnalysisContext context, IEnumerable<ParameterSyntax> parameters)
        {
            this.context = context;

            this.parameterSyntax = parameters.ToDictionary(ps => ps.Identifier.Text);

            // We don't actually need the type of the parameter
            this.parameterNames = this.parameterSyntax.Keys.ToDictionary(
                k => k,
                _ => typeof(object));
        }

        public void Analyze(AttributeSyntax attribute)
        {
            string url = this.GetRouteUrl(attribute);
            if (url != string.Empty)
            {
                this.ParseUrl(url, this.parameterNames, this.optionalParameters);
            }
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

        protected override void OnQueryParameter(string key, Type parameterType, string name)
        {
        }

        private static DiagnosticDescriptor GetDiagnostic(ErrorType error)
        {
            switch (error)
            {
                case ErrorType.DuplicateParameter:
                    return RouteAnalyzer.DuplicateCaptureRule;

                case ErrorType.MissingClosingBrace:
                    return RouteAnalyzer.MissingClosingBraceRule;

                default:
                    return null;
            }
        }

        private string GetRouteUrl(AttributeSyntax attribute)
        {
            AttributeArgumentSyntax argument = attribute.ArgumentList.Arguments.FirstOrDefault();
            var literal = argument?.Expression as LiteralExpressionSyntax;
            if (literal == null)
            {
                return string.Empty;
            }

            this.currentNode = literal.Token;
            return this.currentNode.ValueText;
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
