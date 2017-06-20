Write-Host "Restoring packages..."
dotnet restore /nologo


Write-Host "Building projects..."
foreach ($project in (dir .\src -Name -Recurse *.csproj))
{
	dotnet build -c Release src\$project /nologo
}

Write-Host "Building unit tests..."
foreach ($project in (dir .\test -Name -Recurse *.csproj))
{
	dotnet build -c Debug test\$project /nologo
}

Write-Host "Creating NuGet packages..."
foreach ($project in (dir .\src -Name -Recurse *.csproj))
{
	dotnet pack -c Release src\$project /nologo --no-build --version-suffix=$env:APPVEYOR_BUILD_NUMBER /p:NoPackageAnalysis=true
}