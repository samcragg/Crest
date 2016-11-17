$files = "Container.cs", "ImTools.cs"

foreach ($file in $files)
{
    $path = Join-Path $env:userprofile ".nuget/packages/DryIoc.Internal/2.9.0/contentFiles/cs/any"
    $path = Join-Path $path $file
    if (!(Test-Path ($path + ".original")))
    {
        Copy-Item $path -Destination ($path + ".original")

        # The Get-Contents/Set-Contents combo seems to nuke newlines (with the
        # encoding set to UTF8) so use the .NET versions instead
        $contents = [io.file]::ReadAllText($path)
        [io.file]::WriteAllText($path, "// <auto-generated/>`n" + $contents)
    }
}
