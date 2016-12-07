namespace Analyzers.UnitTests
{
    /// <summary>
    /// Contains code snippets.
    /// </summary>
    internal static class Code
    {
        internal const string Usings = "using System; using System.Threading.Tasks; ";

        internal const string DeleteAttribute = "[System.AttributeUsage(System.AttributeTargets.Method)]public class DeleteAttribute : System.Attribute { public DeleteAttribute(string route) { } } ";

        internal const string GetAttribute = "[System.AttributeUsage(System.AttributeTargets.Method)]public class GetAttribute : System.Attribute { public GetAttribute(string route) { } } ";

        internal const string PostAttribute = "[System.AttributeUsage(System.AttributeTargets.Method)]public class PostAttribute : System.Attribute { public PostAttribute(string route) { } } ";

        internal const string PutAttribute = "[System.AttributeUsage(System.AttributeTargets.Method)]public class PutAttribute : System.Attribute { public PutAttribute(string route) { } } ";

        internal const string RouteAttributes = DeleteAttribute + GetAttribute + PostAttribute + PutAttribute;

        internal const string VersionAttribute = "[System.AttributeUsage(System.AttributeTargets.Method)] public class VersionAttribute : System.Attribute { public VersionAttribute(int from, int to = int.MaxValue) { From = from; To = to; } public int From { get; } public int To { get; } } ";
    }
}
