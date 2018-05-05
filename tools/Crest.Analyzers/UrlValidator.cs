namespace Crest.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Crest.Host.Routing;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal sealed class UrlValidator : UrlParser
    {
        private readonly IReadOnlyDictionary<string, Type> parameterNames;
        private readonly IReadOnlyDictionary<string, ParameterSyntax> parameterSyntax;
        private readonly ISet<string> optionalParameters = new SortedSet<string>();
        private readonly SyntaxNodeAnalysisContext context;
        private CSharpSyntaxNode currentNode;

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
        }

        protected override void OnLiteralSegment(string value)
        {
        }

        protected override void OnQueryParameter(string key, Type parameterType, string name)
        {
        }

        private string GetRouteUrl(AttributeSyntax attribute)
        {
            AttributeArgumentSyntax argument = attribute.ArgumentList.Arguments.FirstOrDefault();
            if (argument == null)
            {
                return string.Empty;
            }

            this.currentNode = argument;
            Optional<object> value = this.context.SemanticModel.GetConstantValue(argument.Expression);
            return value.HasValue ? value.Value.ToString() : string.Empty;
        }

        private static DiagnosticDescriptor GetDiagnostic(ErrorType error)
        {
            switch (error)
            {
                case ErrorType.DuplicateParameter:
                    return RouteAnalyzer.DuplicateCaptureRule;

                default:
                    return null;
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
