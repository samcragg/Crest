# Internal Implementation

These are some general notes on how some of the internal classes are structured
to get an understanding of how the inner workings of the code is organised.

# General

The library should be extendable, hence there are a lot of interfaces with a
single class implementing them. If multiple providers can be used then they
should have a way of ordering them so that they can be invoked in a configurable
order. The convention is to use an `Order` property when all the providers will
be invoked and a `Priority` property when they will be invoked until some kind
of match is found (i.e. they may not all be invoked).

# Conversion

The classes in this namespace allow the conversion from .NET objects to bytes
depending on what the client can understand (i.e. content negotiation). The
`MediaRange` is able to parse the value from the `Accept` header, which is used
by the `ContentConverterFactory` class to determine which factory to use to
generate the bytes.

# Engine

These are mainly types that allow customisation of the library for advanced
scenarios and placed here to keep the root namespace tidy. Most interfaces will
have a default implementation by the library, however, the interface is exposed
to allow custom types to be substituted by client code.

# Routing

Routes are scanned at start-up by the `DiscoveryService`, which converts them to
metadata containing the version, verb, path and method to invoke. The method,
along with a factory to create the instance, is wrapped up by `RouteMethodAdapter`
into a `RouteMethod` delegate with some LINQ expressions, so that a simple
delegate can be used to invoke the route if it matches a request.

The actual route path is parsed by `NodeBuilder`, which also does a check for
ambiguous routes. It produces a sequence of `IMatchNode` objects that match
individual segments of the route, such as literals. A segment is simply the part
between the forward slashes (i.e. /segment/) and these matchers will be used when
handling requests to parse the URL. The matchers are combined in the `RouteMapper`
to form a pseudo tree for each HTTP verb, with `RouteNode{T}` used to form the
nodes of the tree. Identical matchers are combined into a single node, with
values being stored on nodes that have match a full route, i.e. given two routes:

    /example/first  -> MyFirst()
    /example/second -> MySecond()

This would produce three nodes, with the same node being used to match `example`
in both the routes:

                  example
               (Value: null)
                     |
           ---------------------
           |                   |
         first               second
    (Value: MyFirst)    (Value: MySecond)

The `RouteNode{T}`s stored by `RouteMapper` hold the version information, so
multiple methods for the same path can be stored together and depending on the
version of the request (which is stored against a special key in the captured
parameters dictionary). Storing the routes in this arrangement should allow for
quickly finding a match as we don't have to do a linear search against all known
routes for each request.
