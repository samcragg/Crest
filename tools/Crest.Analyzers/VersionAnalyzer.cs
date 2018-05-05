namespace Crest.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class VersionAnalyzer : DiagnosticAnalyzer
    {
        public const string MissingVersionAttributeId = "MissingVersionAttribute";
        public const string VersionOutOfRangeId = "VersionOutOfRange";

        private static readonly DiagnosticDescriptor MissingVersionAttributeRule =
            new DiagnosticDescriptor(
                MissingVersionAttributeId,
                "Missing version attribute",
                "The version attribute is required.",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "All routes must have a version specified.");

        private static readonly DiagnosticDescriptor VersionOutOfRangeRule =
            new DiagnosticDescriptor(
                VersionOutOfRangeId,
                "Invalid version range",
                "The version range is not valid.",
                "Syntax",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "The from version must be greater than zero and less than or equal to the to version.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingVersionAttributeRule, VersionOutOfRangeRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static bool IsRangeValid(int? minimum, int? maximum)
        {
            if (minimum == null)
            {
                return true;
            }

            if (minimum.Value < 1)
            {
                return false;
            }

            if (maximum == null)
            {
                return true;
            }

            return minimum.Value <= maximum.Value;
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            if (RouteAttributeInfo.GetRouteAttributes(method).Any())
            {
                AttributeSyntax version = method.AttributeLists.SelectMany(a => a.Attributes)
                                                .FirstOrDefault(a => a.Name.ToString() == "Version");

                if (version == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(MissingVersionAttributeRule, method.Identifier.GetLocation()));
                }
                else
                {
                    this.VerifyVersionRange(context, version);
                }
            }
        }

        private void VerifyVersionRange(SyntaxNodeAnalysisContext context, AttributeSyntax version)
        {
            AttributeArgumentSyntax minimumArg =
                version.ArgumentList.Arguments.FirstOrDefault();

            AttributeArgumentSyntax maximumArg =
                version.ArgumentList.Arguments.LastOrDefault();

            int? minimum = null;
            int? maximum = null;
            if (minimumArg != null)
            {
                minimum = context.SemanticModel.GetConstantValue(minimumArg.Expression).Value as int?;

                if ((maximumArg != null) && (maximumArg != minimumArg))
                {
                    maximum = context.SemanticModel.GetConstantValue(maximumArg.Expression).Value as int?;
                }
            }

            if (!IsRangeValid(minimum, maximum))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(VersionOutOfRangeRule, version.ArgumentList.GetLocation()));
            }
        }
    }
}
