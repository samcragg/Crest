# Content Converters

To enable the .NET objects to be returned to the client in the a format the
client understands, the object is converted using
[content negotiation](Content Negotiation.md). Additional MIME types can be
supported by implementing the `IContentConverter` interface.

## Default implementations

By default, the framework ships with converters for handling the following:

| Type  | MIME type             | Priority |
|-------|-----------------------|:--------:|
| HTML  | text/html             |   500    |
| JSON  | application/json      |   500    |
| XHTML | application/xhtml+xml |   500    |

## Custom implementations

To create a converter for another type, simple implement the `IContentConverter`
interface and it will be picked up by the default factory. To override one of
the default implementations, make sure that the `Priority` property returns a
value higher than the default one (see the above table).

**Note:** The `Stream` passed in to the `WriteTo` method **must not** be
disposed of, as the data written to it needs to be read from it. If using a
`StreamReader` ensure the constructor overload that accepts the `leaveOpen`
option is used, with this parameter being set to `true`.
