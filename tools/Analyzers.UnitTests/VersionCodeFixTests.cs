namespace Analyzers.UnitTests
{
    using System.Threading.Tasks;
    using Analyzers.UnitTests.Helpers;
    using Crest.Analyzers;
    using NUnit.Framework;

    [TestFixture]
    public sealed class VersionCodeFixTests : CodeFixVerifier<VersionAnalyzer, VersionCodeFix>
    {
        [Test]
        public async Task ShouldAddTheVersionAttribute()
        {
            const string Original = Code.Usings + Code.GetAttribute + Code.VersionAttribute + @"
interface IRoute
{
    [Get("""")]
    Task Method();
}";

            const string Fixed = Code.Usings + Code.GetAttribute + Code.VersionAttribute + @"
interface IRoute
{
    [Get("""")]
    [Version(1)]
    Task Method();
}";
            await this.VerifyFix(Original, Fixed);
        }
    }
}
