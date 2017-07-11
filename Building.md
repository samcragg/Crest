# Building

The solutions can be opened in Visual Studio 2017 or compiled with the .NET Core
tools from the command line. All the build scripts used by the CI server are
located in the `\build` directory and can be run locally if the .NET Core tools
are installed and npm (this is required to restore the dependencies for the
`Crest.OpenApi` project). Please make sure the dependencies have been restored
_before_ building (see the section below).

To run the script, open the PowerShell window to the project's root directory and
run:

    .\build\build_all.ps1

The build will produce NuGet packages but won't run any tests. To run the unit
tests (and generate a code coverage report), ensure the PowerShell window is
still in the root directory and run:

    .\build\run_tests.ps1

If all the tests pass then at the end the browser will open showing the code
coverage results.

# Dependencies

Before you can build the `Crest.OpenApi` project (either at the command line or
via Visual Studio), it's embedded resources need to be generated (note this is
is a manual step and **not done** as part of the `build_all.ps1` script). This
can be done by running the `.\build\restore_swagger_ui.ps1` script (as with the
build all script, this must be ran in the project's root directory). This
requires that `npm` is installed on the machine and setup in the PATH correctly.
