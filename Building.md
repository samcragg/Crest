# Building

The solutions can be opened in Visual Studio 2017 or compiled with the .NET Core
tools from the command line. The [Cake](https://www.cakebuild.net/) build
scripts used by the CI server are located in the `\build` directory and can be
run locally if the .NET Core tools are installed and npm (this is required to
restore the dependencies for the`Crest.OpenApi` project).

To run the script, open the PowerShell window to the `build` directory and run:

    .\build.ps1

On Linux there is a `build.sh` script that can be ran instead.

This will restore all the dependencies, build all the solutions, run the unit
tests, get the unit test coverage of the main Crest solution (i.e. not any of
the tools) and then generate a report for the code coverage (this will be saved
to `build\coverage_report\index.htm`)
