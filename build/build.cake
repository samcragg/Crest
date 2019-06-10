#addin nuget:?package=Cake.Compression&version=0.2.2
#addin nuget:?package=Cake.Coverlet
#addin nuget:?package=SharpZipLib&version=1.1.0

#load "utilities.cake"

const string UnitTestCoverageFolder = "./coverage_results/";
const string MainSolution = "../Crest.sln";
const string SwaggerUIFolder = "src/Crest.OpenApi/SwaggerUI/";

string configuration = Argument("configuration", "Release");
bool isLocalBuild = BuildSystem.IsLocalBuild;
DirectoryPath sonarTool = null;
string target = Argument("target", "Default");

string version = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
string informationVersion = version;
if (string.IsNullOrEmpty(version))
{
    version = "0.1.0";
    string build = EnvironmentVariable("APPVEYOR_BUILD_NUMBER") ?? "local";
    informationVersion = "0.1.0-" + build;
}

var msBuildSettings = new DotNetCoreMSBuildSettings
{
    NoLogo = true
};
msBuildSettings.Properties["PackageVersion"] = new[] { informationVersion };

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
    .IsDependentOn("CreateAssemblyInfo")
    .Does(() =>
{
    DotNetCoreBuild(MainSolution, buildSettings);
});

Task("BuildAndTestTools")
    .IsDependentOn("CreateAssemblyInfo")
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

Task("CreateAssemblyInfo")
    .Does(() =>
{
    Information("Creating AssemblyInfo with version '{0}'...", informationVersion);
    CreateAssemblyInfo("../src/AssemblyInfo.cs", new AssemblyInfoSettings {
        FileVersion = version,
        InformationalVersion = informationVersion,
        Version = version,
    });
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

Task("Pack")
    .Does(() =>
{
    string[] projects = new[]
    {
        "../src/Crest.Abstractions/Crest.Abstractions.csproj",
        "../src/Crest.Core/Crest.Core.csproj",
		"../src/Crest.DataAccess/Crest.DataAccess.csproj",
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
        OutputDirectory = "./artifacts/"
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
    Information("Downloading package...");
    var swagger = DownloadFile("https://registry.npmjs.org/swagger-ui-dist/-/swagger-ui-dist-3.22.3.tgz");

    Information("Expanding package...");
    GZipUncompress(swagger, "./swagger-ui-dist");

    string[] files = new[]
    {
        "./swagger-ui-dist/package/swagger-ui.css",
        "./swagger-ui-dist/package/swagger-ui-bundle.js",
        "./swagger-ui-dist/package/swagger-ui-standalone-preset.js"
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
    var settings = new ReportGeneratorSettings
    {
        Verbosity = ReportGeneratorVerbosity.Error
    };
    ReportGenerator(GetFiles(UnitTestCoverageFolder + "*.xml"), "./coverage_report", settings);
	
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
    sonarTool = InstallDotNetTool("dotnet-sonarscanner", "4.5.0 ");

    string unitTestReports = (new DirectoryPath(UnitTestCoverageFolder)).FullPath + "/*.xml";
    var arguments = "/k:\"crest\""
    + " /d:sonar.organization=\"samcragg-github\""
    + " /d:sonar.host.url=\"https://sonarcloud.io\""
    + " /d:sonar.login=\"" + EnvironmentVariable("SONAR_TOKEN") + "\""
    + " /d:sonar.cs.opencover.reportsPaths=\"" + unitTestReports + "\""
    + " /d:sonar.exclusions=file:**/" + SwaggerUIFolder + "*";
    int exitCode = StartProcess(sonarTool.CombineWithFilePath("dotnet-sonarscanner.exe"), "begin " + arguments);
    if (exitCode != 0)
    {
        throw new Exception("Unable to begin Sonar analysis");
    }
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
    int exitCode = StartProcess(
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

    if (exitCode != 0)
    {
        throw new Exception("Unable to finish Sonar analysis");
    }
});

Task("UnitTestWithCover")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = CreateUnitTestSettings();
    RunTestWithCoverage(UnitTestCoverageFolder, GetFiles("../test/**/*.csproj"), settings);
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
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("SonarEnd")
    .IsDependentOn("UploadTestReport")
    .IsDependentOn("Pack");

RunTarget(target);
