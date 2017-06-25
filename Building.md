# Building

The solutions can be opened in Visual Studio 2017 or compiled with the .NET Core
tools from the command line. All the build scripts used by the CI server are
located in the `\build` directory and can be run locally if the .NET Core tools
are installed.

To run the script, open the PowerShell window to the project's root directory and
run:

    .\build\build_all.ps1

The build will produce NuGet packages but won't run any tests. To run the unit
tests (and generate a code coverage report), ensure the PowerShell window is
still in the root directory and run:

    .\build\run_tests.ps1

If all the tests pass then at the end the browser will open showing the code
coverage results.
