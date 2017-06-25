$version = "local"
if (Test-Path env:\APPVEYOR_BUILD_NUMBER)
{
    $version = $env:APPVEYOR_BUILD_NUMBER
}

$solutions = @(
    ".\Crest.sln",
    ".\tools\Crest.OpenApi.Generator.sln"
)
foreach ($solution in $solutions)
{
    Write-Host ("Building " + $solution)

    Write-Host "Restoring packages..."
    dotnet restore $solution /nologo -v q

    Write-Host "Building..."
    dotnet build -c Release $solution /nologo
}

Write-Host "Creating NuGet packages..."
$projects = @(
    ".\src\Crest.Abstractions\Crest.Abstractions.csproj",
    ".\src\Crest.Core\Crest.Core.csproj",
    ".\src\Crest.Host\Crest.Host.csproj",
    ".\src\Crest.Host.AspNetCore\Crest.Host.AspNetCore.csproj",
    ".\src\Crest.OpenApi\Crest.OpenApi.csproj",
    ".\tools\Crest.OpenApi.Generator\Crest.OpenApi.Generator.csproj"
)
foreach ($project in $projects)
{
    dotnet pack -c Release $project /nologo --no-build --version-suffix=$version /p:NoPackageAnalysis=true
}