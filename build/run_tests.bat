dotnet restore .\build\project.json --packages .\build\tools

WHERE dotnet > .\build\dotnet.path
SET /p dotnetexe=<.\build\dotnet.path

.\build\tools\OpenCover\4.6.519\tools\OpenCover.Console.exe ^
-output:.\CoverResult.xml ^
-hideskipped ^
-oldstyle ^
-register:user ^
-filter:+[Crest.Core]* ^
-target:"%dotnetexe%" ^
-targetargs:"test test\Core.UnitTests --no-build"


.\build\tools\coveralls.net\0.7.0\tools\csmacnz.Coveralls.exe ^
--opencover ^
-i .\CoverResult.xml ^
--repoTokenVariable COVERALLS_REPO_TOKEN ^
--useRelativePaths ^
--serviceName appveyor ^
--commitId "%APPVEYOR_REPO_COMMIT%" ^
--commitBranch "%APPVEYOR_REPO_BRANCH%" ^
--commitAuthor "%APPVEYOR_REPO_COMMIT_AUTHOR%" ^
--commitEmail "%APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL%" ^
--commitMessage "%APPVEYOR_REPO_COMMIT_MESSAGE%" ^
--jobId "%APPVEYOR_BUILD_NUMBER%"
