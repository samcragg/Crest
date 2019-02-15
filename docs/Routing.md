# Routing

Crest uses attributes to know how to map between the route and the handling
method (this allows explicit mapping rather than convention based). In order
for the mappings to picked up, the methods need to be declared on an interface.

Multiple attributes of the same HTTP verb can be applied to the same method,
allowing multiple routes to be handled by the same handler. Also, the same URL
can be applied to different methods as long as they have different version
ranges.

The URLs in the attributes follow the format of
[RFC 6570](https://tools.ietf.org/html/rfc6570), which should enable client
applications consuming them to be able to easily parse the same value.

## Literal matching

Any text in a route is matched literally (ignoring case):

``` C#
[Get("/route")]
[Version(1)]
Task GetRoute();
```

The above would match a request for `/v1/Route` and `/v1/route` etc

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
[Get("/widget{?name}")]
[Version(1)]
Task FindWidget(string name = null);
```

If a default value is specified and the query parameter is not sent in the
request, then that value will be injected instead:

``` C#
[Get("/widget{?name}")]
[Version(1)]
Task FindWidget(string name = "*");
```

Calling `/v1/widget` would result in the above method being invoked with "*" as
the `name` parameter value.

### Boolean query values

Normally query values are passed in the URL in the format `key=value`, however,
for `bool` parameters then a presence only match is done:

``` C#
[Get("/widget{?all}")]
[Version(1)]
Task FindWidget(bool all = false);
```

Calling `/v1/widget?all` would pass in `true` for the `all` parameter, as well
as `/v1/widget?all=1` or `/v1/widget?all=true`.

### Any query key/value

As the query part of the URL can be dynamic, there may be scenarios where you
want to capture what has been sent in the request. To enable this, you can use
a `dynamic` type for the parameter and place a `*` after its name in the URL;
the passed in value will have a member for each key that has not been captured:

``` C#
[Get("/widget{?count,properties*}")]
[Version(1)]
Task FindWidget(dynamic properties, int count = 10);
```

Calling the above via the URL `/v1/widget?name=value&count=2` would invoke the
method with `properties` having a member called `name` and the `count` parameter
equal to `2`. If you try to access a member on the dynamic type that wasn't
specified in the request then it will return null rather than throwing an
exception. The return type of the dynamic member can be assigned to either a
`string` (the first item in the query will be used when there are multiple keys)
or `IEnumerable<string>` (including `string[]`). If it is assigned to any other
type (e.g. an `int`) then the string will be attempted to be converted to the
specified type, however, this will throw if the format is incorrect.

Take a look at [filtering and sorting](Filtering%20and%20Sorting.md) for a good
use case of the any matcher.

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
