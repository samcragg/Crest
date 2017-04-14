# Get all the NuGet packages
dotnet restore

# Build the project in src as release
foreach ($project in (dir .\src -Name -Recurse *.csproj))
{
	dotnet build -c Release src\$project
}

# Build the projects in test
foreach ($project in (dir .\test -Name -Recurse *.csproj))
{
	dotnet build test\$project
}
