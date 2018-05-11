using System.IO.Compression;

public class NpmInstallSettingsWithPrefix : NpmInstallSettings
{
    protected override void EvaluateCore(ProcessArgumentBuilder args)
    {
        base.EvaluateCore(args);
        args.Append("--prefix .");
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
