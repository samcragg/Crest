# Content Negotiation

When a client asks for the resource it can specify the _representation_ of that
resource that it wants, such as JSON or XML etc. This is done by providing an
`Accept` header with the various MIME types or resources that the client can
process.

Content negotiation is used by Crest to serialize the .NET objects returned by
the route methods into a format understood by the client.

## Default behaviour

By default, responses can be sent in JSON format or XML format but this can
easily be extended. If the `Accept` header is not present then the system
defaults in the value `application/json` and processes the response in the
normal manner.

The media types are checked in order of their weight (the `q` parameter), with
higher values being checked first to see if any of the serializers can produce
that type. If no suitable serializer can be found for any of the media types
in the `Accept` header then a `406 No Content` will be sent back to the client.

## Customising

There are two main interfaces that the system uses: one for performing the
serialization (`IContentConverter`) and one for determining which serializer to
use (`IContentConverterFactory`).

The default factory gets all implementations of the `IContentConverter`
interface injected in to the constructor and checks what MIME types they can
handle, using the `Priority` as a tie-breaker if two serializers can produce the
same type. The MIME type returned can also specify the weight of the mapping,
allowing it to act as a fall-back if another serializer is not found. For
example, if `ConverterA` returns `application/type;q=0.5` and `ConverterB`
return `application/type` then `ConverterB` would be used for `application/type`
_even if_ `ConverterA` has a higher priority.

**Note:** If the client specifies any type (`*/*`), the serializer with the
highest priority will be used.
