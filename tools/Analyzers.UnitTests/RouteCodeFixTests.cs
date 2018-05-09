namespace Analyzers.UnitTests
{
    using System.Threading.Tasks;
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Xunit;

    public sealed class RouteCodeFixTests : CodeFixVerifier<RouteAnalyzer, RouteCodeFix>
    {
        [Fact]
        public async Task ShouldAddTheVersionAttribute()
        {
            const string Original = Code.Usings + Code.GetAttribute + Code.VersionAttribute + @"
interface IRoute
{
    [Get(""open{brace"")]
    Task Method();
}";

            const string Fixed = Code.Usings + Code.GetAttribute + Code.VersionAttribute + @"
interface IRoute
{
    [Get(""open{{brace"")]
    Task Method();
}";
            await this.VerifyFix(Original, Fixed);
        }
    }
}
