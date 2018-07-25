#addin nuget:?package=Cake.Npm
#load "utilities.cake"

const string IntegrationTestCoverageFolder = "./coverage_it_results/";
const string UnitTestCoverageFolder = "./coverage_results/";
const string MainSolution = "../Crest.sln";
const string SwaggerUIFolder = "src/Crest.OpenApi/SwaggerUI/";

string configuration = Argument("configuration", "Release");
bool isLocalBuild = BuildSystem.IsLocalBuild;
DirectoryPath sonarTool = null;
string target = Argument("target", "Default");
string version = isLocalBuild ? "local" : EnvironmentVariable("APPVEYOR_BUILD_NUMBER");

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    NoLogo = true
};

var buildSettings = new DotNetCoreBuildSettings
{
    Configuration = configuration,
    MSBuildSettings = msBuildSettings,
    NoRestore = true
};

DotNetCoreTestSettings CreateUnitTestSettings()
{
    // Verbosity = DotNetCoreVerbosity.Quiet doesn't seem to work, so we'll add
    // it ourselves
    return new DotNetCoreTestSettings
    {
        ArgumentCustomization = args => args.Append("--verbosity quiet"),
        Configuration = configuration,
        Filter = "Category!=Integration",
        NoBuild = true,
        NoRestore = true
    };
}

Task("Build")
    .Does(() =>
{
    DotNetCoreBuild(MainSolution, buildSettings);
});

Task("BuildAndTestTools")
    .Does(() =>
{
    foreach (var solution in GetFiles("../tools/*.sln"))
    {
        DotNetCoreBuild(solution.FullPath, buildSettings);
    }

    var settings = CreateUnitTestSettings();
    foreach (var project in GetFiles("../tools/*.UnitTests/*.csproj"))
    {
        DotNetCoreTest(project.FullPath, settings);
    }
});

Task("ExcludeExternalFilesFromAnalysis")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var packagesDirectory = GetNugetPackageDirectory();
    AddAutoGeneratedTags(GetFiles(packagesDirectory.FullPath + "/dryioc.internal/*/contentFiles/cs/*/DryIoc/*.cs"));
    AddAutoGeneratedTags(GetFiles(packagesDirectory.FullPath + "/liblog/*/contentFiles/cs/netstandard2.0/**/*.pp"));

    // Temporary fix to hide all LibLog types
    // public class
    foreach (var file in GetFiles(packagesDirectory.FullPath + "/liblog/*/contentFiles/cs/netstandard2.0/**/*.pp"))
    {
        System.IO.File.WriteAllText(
            file.FullPath,
            System.Text.RegularExpressions.Regex.Replace(System.IO.File.ReadAllText(file.FullPath), "public class", "internal class"));
    }
});

Task("IntegrationTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = CreateUnitTestSettings();
    settings.Filter = "Category=Integration";

    Parallel.ForEach(GetFiles("../test/**/*.csproj"), project =>
    {
        // Specify the path to the test adapter to avoid a warning that no tests
        // were discovered because we filtered them all out
        settings.TestAdapterPath = project.GetDirectory().CombineWithFilePath("./bin/" + configuration + "/netcoreapp2.0/").FullPath;
        DotNetCoreTest(project.FullPath, settings);
    });
});

Task("IntegrationTestWithCover")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = CreateUnitTestSettings();
    settings.Filter = "Category=Integration";
    RunOpenCover(IntegrationTestCoverageFolder, GetFiles("../test/**/*.csproj"), settings);
});

Task("Pack")
    .Does(() =>
{
    string[] projects = new[]
    {
        "../src/Crest.Abstractions/Crest.Abstractions.csproj",
        "../src/Crest.Core/Crest.Core.csproj",
        "../src/Crest.Host/Crest.Host.csproj",
        "../src/Crest.Host.AspNetCore/Crest.Host.AspNetCore.csproj",
        "../src/Crest.OpenApi/Crest.OpenApi.csproj",
        "../tools/Crest.Analyzers/Crest.Analyzers.csproj",
        "../tools/Crest.OpenApi.Generator/Crest.OpenApi.Generator.csproj"
    };

    var packSettings = new DotNetCorePackSettings
    {
        Configuration = configuration,
        MSBuildSettings = msBuildSettings,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = "./artifacts/",
        VersionSuffix = version
    };

    Parallel.ForEach(projects, project =>
    {
         DotNetCorePack(project, packSettings);
    });
});

Task("RestorePackages")
    .Does(() =>
{
    DotNetCoreRestore(MainSolution);

    // We have to use the old style restore for the VSIX project so that the
    // tasks are downloaded for the rest of the dotnet commands to work
    NuGetRestore("../tools/Crest.Analyzers.Vsix/packages.config", new NuGetRestoreSettings
    {
        PackagesDirectory = "../tools/packages"
    });

    foreach (var solution in GetFiles("../tools/*.sln"))
    {
        DotNetCoreRestore(solution.FullPath);
    }
});

Task("RestoreSwaggerUI")
    .Does(() =>
{
    var settings = new NpmInstallSettingsWithPrefix();
    settings.AddPackage("swagger-ui-dist", "3.0.18");
    NpmInstall(settings);

    string[] files = new[]
    {
        "./node_modules/swagger-ui-dist/swagger-ui.css",
        "./node_modules/swagger-ui-dist/swagger-ui-bundle.js",
        "./node_modules/swagger-ui-dist/swagger-ui-standalone-preset.js"
    };

    foreach (string file in files)
    {
        CompressFile(file, "../" + SwaggerUIFolder);
    }
});

Task("ShowTestReport")
    .WithCriteria(isLocalBuild)
    .IsDependentOn("UnitTestWithCover")
    .Does(() =>
{
    ReportGenerator(GetFiles(UnitTestCoverageFolder + "*.xml"), "./coverage_report");
    if (IsRunningOnWindows())
    {
        StartAndReturnProcess(
            "cmd",
            new ProcessSettings
            {
                Arguments = ProcessArgumentBuilder.FromString("/c start ./coverage_report/index.htm")
            });
    }
});

Task("SonarBegin")
    .WithCriteria(!isLocalBuild)
    .Does(() =>
{
    sonarTool = InstallDotNetTool("dotnet-sonarscanner", "4.3.1 ");

    string integrationReports = (new DirectoryPath(IntegrationTestCoverageFolder)).FullPath + "/*.xml";
    string unitTestReports = (new DirectoryPath(UnitTestCoverageFolder)).FullPath + "/*.xml";
    var arguments = "/k:\"crest\""
    + " /d:sonar.organization=\"samcragg-github\""
    + " /d:sonar.host.url=\"https://sonarcloud.io\""
    + " /d:sonar.login=\"" + EnvironmentVariable("SONAR_TOKEN") + "\""
    + " /d:sonar.cs.opencover.reportsPaths=\"" + unitTestReports + "\""
    + " /d:sonar.cs.opencover.it.reportsPaths=\"" + integrationReports + "\""
    + " /d:sonar.exclusions=\"" + SwaggerUIFolder + "**\"";
    StartProcess(sonarTool.CombineWithFilePath("dotnet-sonarscanner.exe"), "begin " + arguments);
});

Task("SonarEnd")
    .WithCriteria(!isLocalBuild)
    .Does(() =>
{
    // The output is very verbose, so we'll capture it and filter it
    var settings = new ProcessSettings
    {
        Arguments = "end /d:sonar.login=\"" + EnvironmentVariable("SONAR_TOKEN") + "\"",
        RedirectStandardError = true,
        RedirectStandardOutput = true
    };
    StartProcess(
        sonarTool.CombineWithFilePath("dotnet-sonarscanner.exe"),
        settings,
        out IEnumerable<string> output,
        out IEnumerable<string> error);

    foreach (string line in output.Where(l => !l.StartsWith("INFO:")))
    {
        Information(line);
    }

    foreach (string line in error)
    {
        Error(line);
    }
});

Task("UnitTestWithCover")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = CreateUnitTestSettings();
    RunOpenCover(UnitTestCoverageFolder, GetFiles("../test/**/*.csproj"), settings);
});

Task("UploadTestReport")
    .WithCriteria(!isLocalBuild)
    .IsDependentOn("UnitTestWithCover")
    .Does(() =>
{
    DirectoryPath toolPath = InstallDotNetTool("coveralls.net", "1.0.0");

    Information("Uploading reports");
    var inputFiles = string.Join(";", GetFiles(UnitTestCoverageFolder + "*.xml").Select(f => "opencover=" + f.FullPath));

    var arguments = "--multiple"
    + " --input " + inputFiles
    + " --useRelativePaths"
    + " --commitId " + EnvironmentVariable("APPVEYOR_REPO_COMMIT")
    + " --commitBranch " + EnvironmentVariable("APPVEYOR_REPO_BRANCH")
    + " --commitAuthor \"" + EnvironmentVariable("APPVEYOR_REPO_COMMIT_AUTHOR") + "\""
    + " --commitEmail " + EnvironmentVariable("APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL")
    + " --commitMessage \"" + EnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE") + "\""
    + " --jobId " + EnvironmentVariable("APPVEYOR_BUILD_NUMBER")
    + " --repoTokenVariable COVERALLS_REPO_TOKEN";
    StartProcess(toolPath.CombineWithFilePath("csmacnz.Coveralls.exe"), arguments);
});

Task("Restore")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("ExcludeExternalFilesFromAnalysis")
    .IsDependentOn("RestoreSwaggerUI");

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildAndTestTools")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTestWithCover")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("ShowTestReport");

Task("Full")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildAndTestTools")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTestWithCover")
    .IsDependentOn("IntegrationTestWithCover")
    .IsDependentOn("SonarEnd")
    .IsDependentOn("UploadTestReport")
    .IsDependentOn("Pack");

RunTarget(target);
