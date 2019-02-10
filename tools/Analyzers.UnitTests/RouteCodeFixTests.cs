namespace Analyzers.UnitTests
{
    using System.Threading.Tasks;
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using Xunit;

    public sealed class RouteCodeFixTests : CodeFixVerifier<RouteAnalyzer, RouteCodeFix>
    {
        [Fact]
        public async Task ShouldAddUnknownParameters()
        {
            const string Original = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""{unknown}"")]
    Task Method();
}";

            const string Fixed = Code.Usings + Code.GetAttribute + @"
interface IRoute
{
    [Get(""{unknown}"")]
    Task Method(string unknown);
}";
            await this.VerifyFix(Original, Fixed);
        }
    }
}
