using System.IO.Compression;

public class NpmInstallSettingsWithPrefix : NpmInstallSettings
{
    protected override void EvaluateCore(ProcessArgumentBuilder args)
    {
        base.EvaluateCore(args);
        args.Append("--prefix .");
    }
}

void AddAutoGeneratedTags(IEnumerable<FilePath> files)
{
    const string AutoGeneratedTag = "// <auto-generated/>\n#pragma warning disable";
    foreach (FilePath file in files)
    {
        string contents = System.IO.File.ReadAllText(file.FullPath);
        if (!contents.StartsWith(AutoGeneratedTag))
        {
            Information("Modifying {0}", file);
            System.IO.File.WriteAllText(file.FullPath, AutoGeneratedTag + "\n" + contents);
        }
    }
}

void CompressFile(FilePath source, DirectoryPath destination)
{
    byte[] contents = System.IO.File.ReadAllBytes(source.FullPath);
    FilePath output = destination.CombineWithFilePath(source.GetFilename() + ".gz");
    Information("Compressing {0} to {1}", source, output);

    using (var gzipStream = new GZipStream(
        System.IO.File.Create(output.FullPath),
        CompressionLevel.Optimal))
    {
        gzipStream.Write(contents, 0, contents.Length);
    }
}

DirectoryPath GetNugetPackageDirectory()
{
    IEnumerable<string> output;
    StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = "nuget locals global-packages -l --force-english-output",
            RedirectStandardOutput = true
        },
        out output);
    
    return Directory(output.First().Substring(24));
}

DirectoryPath InstallDotNetTool(string name, string version)
{
    Information("Installing " + name);
    var toolPath = new DirectoryPath("./tools/" + name);
    StartProcess("dotnet", "tool install " + name + " --version " + version + " --tool-path \"" + toolPath.FullPath + "\"");
    return toolPath;
}

void RunTestWithCoverage(
    DirectoryPath outputFolder,
    IEnumerable<FilePath> files,
    DotNetCoreTestSettings testSettings)
{
    string[] toExclude =
    {
        "[*]DryIoc.*",
        "[*]FastExpressionCompiler.*",
        "[*]ImTools.*",
        "[*]*.Logging.*"
    };

    EnsureDirectoryExists(outputFolder);
    CleanDirectory(outputFolder);

    foreach (project in files)
    {
        var coveletSettings = new CoverletSettings
        {
            CollectCoverage = true,
            CoverletOutputDirectory = outputFolder,
            CoverletOutputFormat = CoverletOutputFormat.opencover,
            CoverletOutputName = project.GetFilenameWithoutExtension() + ".xml",
            Exclude = new List<string>(toExclude),
        };

        DotNetCoreTest(project.FullPath, testSettings, coveletSettings);
    }
}
