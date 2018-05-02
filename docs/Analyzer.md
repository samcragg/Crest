# Analyzer

Crest.Analyzers allows the analyzing of Crest APIs to catch runtime errors at
compile time. It also allows the basic checking of routes to help them follow
some of the REST principles, offering suggestions to keep them consistent.

## Installation

To install the analyzer, simply reference the NuGet package in the project that
declares your API routes.

## Errors

The following are reported as errors by the analyzer:

### MissingVersionAttribute

All methods that have a route applied to them must also have a version applied
to them so the API is always backwards compatible with earlier releases. Since
the following method doesn't have the version attribute applied to it, it will
trigger the error:

``` C#
[Get(...)]
Task Method();
```

A code fix is available that inserts the attribute for you, producing the
following when applied to the above:

```C#
[Get(...)]
[Version(1)]
Task Method();
```

### VersionOutOfRange

The version attributes allow you to specify a range of versions the API is
available between. This error is flagged when the `from` version is greater than
the `to` version:

```C#
[Get(...)]
[Version(2, 1)]
Task Method();
```
