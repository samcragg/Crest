namespace Crest.Analyzers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Helper methods for Crest attributes.
    /// </summary>
    internal static class RouteAttributeInfo
    {
        /// <summary>
        /// Gets the HTTP verb represented by the attribute.
        /// </summary>
        /// <param name="attribute">The attribute to get the value from.</param>
        /// <returns>The HTTP verb.</returns>
        public static string GetHttpVerb(AttributeSyntax attribute)
        {
            return GetAttributeName(attribute).ToUpperInvariant();
        }

        /// <summary>
        /// Gets the route attributes applied to the interface method.
        /// </summary>
        /// <param name="method">The method being analyzed.</param>
        /// <returns>The route attributes found on the method.</returns>
        public static IEnumerable<AttributeSyntax> GetRouteAttributes(MethodDeclarationSyntax method)
        {
            if (method.Parent.Kind() != SyntaxKind.InterfaceDeclaration)
            {
                return Enumerable.Empty<AttributeSyntax>();
            }

            return method.AttributeLists.SelectMany(a => a.Attributes)
                                        .Where(IsRouteAttribute);
        }

        /// <summary>
        /// Gets the route string from the attribute.
        /// </summary>
        /// <param name="model">The semantic model of the compilation.</param>
        /// <param name="attribute">The attribute to get the value from.</param>
        /// <returns>
        /// The route represented by the attribute, or null if one wasn't found.
        /// </returns>
        public static string GetRouteString(SemanticModel model, AttributeSyntax attribute)
        {
            AttributeArgumentSyntax argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
            if (argument == null)
            {
                return null;
            }

            return (string)model.GetConstantValue(argument.Expression).Value;
        }

        /// <summary>
        /// Creates a location within the route string.
        /// </summary>
        /// <param name="attribute">The route attribute.</param>
        /// <param name="start">
        /// The offset from the start of the route string.
        /// </param>
        /// <param name="length">The length for the location.</param>
        /// <returns>A program location in the specified route.</returns>
        public static Location GetRouteStringLocation(AttributeSyntax attribute, int start, int length)
        {
            ExpressionSyntax stringLiteral = attribute.ArgumentList.Arguments[0].Expression;

            // Allow for " or @"
            int stringStart = 1;
            if (stringLiteral.ToString().StartsWith("@\""))
            {
                stringStart = 2;
            }

            return Location.Create(
                stringLiteral.SyntaxTree,
                new TextSpan(stringLiteral.Span.Start + stringStart + start, length));
        }

        /// <summary>
        /// Determines whether an attribute is used to describe a route.
        /// </summary>
        /// <param name="attribute">The attribute to test.</param>
        /// <returns>
        /// true if the attribute describes a route; otherwise, false.
        /// </returns>
        public static bool IsRouteAttribute(AttributeSyntax attribute)
        {
            switch (GetAttributeName(attribute))
            {
                case "Delete":
                case "Get":
                case "Post":
                case "Put":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether an attribute uses the request body.
        /// </summary>
        /// <param name="attribute">The attribute to test.</param>
        /// <returns>
        /// true if the attribute uses the request body; otherwise, false.
        /// </returns>
        public static bool VerbUsesBody(AttributeSyntax attribute)
        {
            switch (GetAttributeName(attribute))
            {
                case "Post":
                case "Put":
                    return true;

                default:
                    return false;
            }
        }

        private static string GetAttributeName(AttributeSyntax attribute)
        {
            string name = attribute.Name.ToString();
            if (name.EndsWith("Attribute"))
            {
                name.Substring(0, name.Length - 9);
            }

            return name;
        }
    }
}
