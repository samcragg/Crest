# Routing

Crest uses attributes to know how to map between the route and the handling
method (this allows explicit mapping rather than convention based). In order
for the mappings to picked up, the methods need to be declared on an interface.

Multiple attributes of the same HTTP verb can be applied to the same method,
allowing multiple routes to be handled by the same handler. Also, the same URL
can be applied to different methods as long as they have different version
ranges.

## Literal matching

Any text in a route is matched literally (ignoring case) with the exception of
braces. Since braces are used to indicate a parameter capture, they must be
escaped:

``` C#
[Get("/{{route}}")]
[Version(1)]
Task GetRoute();
```

The above would match a request for `/v1/{route}`

## Parameter matching

Parameters can come from the route by placing their name in braces inside the
URL (to aid documentation, parameter name matching is done case-insensitive):

``` C#
[Get("/{id}")]
[Version(1)]
Task GetWidget(int id);
```

The order of the parameters does not need to match the order in the route URL,
however, a parameter must only be captured once and all parameters must be
specified in the URL.

The conversion from the URL string to the parameter type is done by the library
and can be used to overload the URL based on the type passed in:

``` C#
[Get("/{id}")]
[Version(1)]
Task GetWidgetByInteger(int id);

[Get("/{name}")]
[Version(1)]
Task GetWidgetByString(string name);
```

However, the above would not work if one method accepted a `short` and the
other an `int`, as it would be ambiguous which method to invoke for `/v1/123`.
The following types are supported out of the box:

+ `DateTime` (ISO 8601 format, either just a date or a date and time)
+ `Guid`
+ `Timespan` (ISO 8601 format duration or the default .NET timespan format)
+ `bool` (`true`, `false`, `1` or `0`)
+ `decimal`
+ `double`, `float`
+ `byte`, `int`, `long`, `sbyte`, `short`, `uint`, `ulong`, `ushort`
+ `string`
+ Any type with a [TypeConverter](https://docs.microsoft.com/en-gb/dotnet/api/system.componentmodel.typeconverter)
  applied to it

## Query parameters

If a parameter appears as part of the query, the parameter must be optional:

``` C#
[Get("/widget?name={name}")]
[Version(1)]
Task FindWidget(string name = null);
```

If a default value is specified and the query parameter is not sent in the
request, then that value will be injected instead:

``` C#
[Get("/widget?name={name}")]
[Version(1)]
Task FindWidget(string name = "*");
```

Calling `/v1/widget` would result in the above method being invoked with "*" as
the `name` parameter value.

### Boolean query values

Normally query values should be in the format `key=value`, however, for `bool`
parameters then a presence only match is done:

``` C#
[Get("/widget?all={includeAll}")]
[Version(1)]
Task FindWidget(bool includeAll = false);
```

Calling `/v1/widget?all` would pass in `true` for the `includeAll` parameter,
as well as `/v1/widget?all=1` or `/v1/widget?all=true`.

## Request body

Some HTTP verbs allow the sending of the request body (such as POST and PUT).
These can be captured by a parameter by applying the `FromBody` attribute to
it:

``` C#
[Put("/{id}")]
[Version(1)]
Task UpdateOrCreateWidget(int id, [FromBody]Widget widget);
```

If the method accepts a single parameter and that parameter is from the
request body, then the `FromBody` attribute can be omitted (i.e. it is only
required when some parameters come from the URL):

``` C#
[Post("/")]
[Version(1)]
Task SaveWidget(Widget widget);
```

## Versions

Designing an API is not an exact science and every application has unique
requirements. To support the changing of an API, all routes must have a version
attribute applied to them. This ensures that as the API evolves, external code
that has a dependency on the APIs will still be able to function when the
service is updated.

``` C#
[Get("/widget/{id}")]
[Version(1, 1)]
Task<LegacyWidget> GetWidget(int id);

[Get("/widget/{id}")]
[Version(2)]
Task<Widget> GetWidget(Guid id);
```

In the above, the existing clients can still call `/v1/widget/123` and get
the old data, however, newer clients can call `/v2/widget/...` and get the
newer object.
