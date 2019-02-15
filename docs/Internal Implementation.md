# Internal Implementation

> **Internal documentation**
>
> This documents implementation details and is not required for normal usage

These are some general notes on how some of the internal classes are structured
to get an understanding of how the inner workings of the code is organised.

## General

The library should be extendible, hence there are a lot of interfaces with a
single class implementing them. If multiple providers can be used then they
should have a way of ordering them so that they can be invoked in a configurable
order. The convention is to use an `Order` property when all the providers will
be invoked and a `Priority` property when they will be invoked until some kind
of match is found (i.e. they may not all be invoked).

## Conversion

The classes in this namespace allow the conversion from .NET objects to bytes
depending on what the client can understand (i.e. content negotiation). The
`MediaRange` is able to parse the value from the `Accept` header, which is used
by the `ContentConverterFactory` class to determine which factory to use to
generate the bytes.

## Engine

These are mainly types that allow customisation of the library for advanced
scenarios and placed here to keep the root namespace tidy. Most interfaces will
have a default implementation by the library, however, the interface is exposed
to allow custom types to be substituted by client code.

## Routing

Routes are scanned at start-up by the `DiscoveryService`, which converts them to
metadata containing the version, verb, path and method to invoke. The method,
along with a factory to create the instance, is wrapped up by `RouteMethodAdapter`
into a `RouteMethod` delegate with some LINQ expressions, so that a simple
delegate can be used to invoke the route if it matches a request.

The actual route path is parsed by `RouteTrieBuilder`, which also does a check
for ambiguous routes. It produces a `RouteTrie` that matches the URL and returns
all the endpoints for that route. This is to allow for the scenario of multiple
versions/verbs that match the same route.

The `RouteTrie` is stored by `RouteMatcher` (which has a builder to create it).
The `RouteTrie` has a special first node that captures the version information -
this is stored in the dictionary with a known key so that it can be retrieved
after the matching and used to select the correct endpoint to invoke.
