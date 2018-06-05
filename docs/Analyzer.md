# Analyzer

Crest.Analyzers allows the analyzing of Crest APIs to catch runtime errors at
compile time. It also allows the basic checking of routes to help them follow
some of the REST principles, offering suggestions to keep them consistent.

## Installation

To install the analyzer, simply reference the NuGet package in the project that
declares your API routes.

## Errors

The following are reported as errors by the analyzer:

### CannotBeMarkedAsFromBody

Parameters that are captured in the URL cannot be marked as coming from the
request body. The following would trigger the error as the parameter both
appears in the URL and has the `FromBody` attribute applied to it:

```C#
[Put("/{id}")]
Task Method([FromBody]int id);
```

### DuplicateCapture

A parameter capture can only appear once in the route, for example, the
following would trigger the error:

```C#
[Get("/things/{id}/details/{id}")]
Task Method(int id);
```

Because `{id}` appears more than once, it is uncertain which one to use to
provide the value for the parameter, therefore, the route is invalid.

### MissingClosingBrace

The syntax for a parameter capture in the route is `{ + parameterName + }`,
therefore, the following would trigger the error as the end brace is missing:

```C#
[Get("/things/{capture")]
```

### MissingQueryValue

All query parameters must specify a capture as their value so should be in the
form `key={value}`. The following generates the error as only a key is specified:

```C#
[Get("/route?queryKeyOnly")]
```

### MissingVersionAttribute

All methods that have a route applied to them must also have a version applied
to them so the API is always backwards compatible with earlier releases. Since
the following method doesn't have the version attribute applied to it, it will
trigger the error:

```C#
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

### MultipleBodyParameters

Only a single parameter can be marked as soming from the request body, so this
error is raised when more than one parameter is marked as `FromBody`:

```C#
[Put("/")]
Task Method([FromBody]int id, [FromBody]string name);
```

If multiple values are required from the request body, these should be
encapsulated in a plain old data object.

### MustBeOptional

All parameters that are captured in the query must be marked as optional (i.e.
have their default value specified), as the query part of the URL is by
definition optional. The following generates the error as no default value is
specified for the argument:

```C#
[Get("/route?queryKey={id}")]
Task Method(int id);
```

### MustCaptureQueryValue

Query values must be captured by an optional argument in the method. Since the
following specified a literal value for the query value, an error is generated:

```C#
[Get("/route?queryKey=literalValue")]
```

### ParameterNotFound

All method parameters must be captured in the route URL. This error is generated
when a parameter doesn't have a default value and is not specified in the URL:

```C#
[Get("/route")]
Task Method(int id);
```

### UnescapedBrace

Braces are used to indicate a parameter capture, which must appear at the start
of a URL segment (i.e. preceded by a `\`). If the URL is to contain a brace, it
must be escaped by doubling it (i.e. `{{` or `}}`). The following contains a
single brace inside the segment and, therefore, generates the error:

```C#
[Get("/segment1/segment_with_{")]
```

A code fix is available that inserts the brace for you, producing the following
when applied to the above:

```C#
[Get("/segment1/segment_with_{{")]
```

### UnknownParameter

A parameter capture has been specified in the route but no parameter was found
with the same name. Note, as with C#, the name of the parameter in the capture
is case sensitive.

```C#
[Get("/things/{id}")]
Task Method();
```

Since the method does not have any parameters, the above triggers the error. A
code fix is provided that will add the parameter, so applying it to the above
would produce:

```C#
[Get("/things/{id}")]
Task Method(string id);
```

### VersionOutOfRange

The version attributes allow you to specify a range of versions the API is
available between. This error is flagged when the `from` version is greater than
the `to` version:

```C#
[Version(2, 1)]
Task Method();
```
