# Crest

![Icon](https://cdn.rawgit.com/samcragg/Crest/52010cbfabb5892d923d591a419122591a8085a1/docs/images/Icon.svg)

[![Build status](https://ci.appveyor.com/api/projects/status/spal08yea33stdlw/branch/master?svg=true)](https://ci.appveyor.com/project/samcragg/crest/branch/master) [![Coverage Status](https://coveralls.io/repos/github/samcragg/Crest/badge.svg?branch=master)](https://coveralls.io/github/samcragg/Crest?branch=master) ![License](https://img.shields.io/github/license/samcragg/crest.svg)

This library provides a simple way to create versioned RESTful micro-services
built against .NET Core with ease whilst remaining performant.

The HTTP side of the requests/responses is abstracted away as much as possible,
so writing the logic code looks just like a traditional server application,
making unit testing trivial. By hiding away as much as the HTTP request as
possible, multiple micro-services implemented with Crest will have a consistent
feel to them from the end users perspective, giving the feeling of a single
system yet allowing development to occur without tight coupling.

Creating the right API first time is difficult, however, getting third parties
to update their code is even harder. Therefore, there is strong support for
providing versioned routes, to allow for new functionality to be added without
effecting older clients.

# Provided functionality

Out of the box the framework handles most of the mundane functionality to allow
you to get developing your service quickly:

+ Dependency injection with assembly scanning (i.e. it will map your interfaces
  to their concrete implementation for you).
+ Deserializing/serializing the HTTP request/response based on its content type
  (out of the box JSON, URL form encoded data and XML are supported).
+ OpenAPI documentation of endpoints
+ Health page
+ Basic metrics for the last 15 minutes
+ JWT handling

# Basic usage

First create an interface that describes the routes. The XML documentation will
be converted to an [OpenAPI](https://www.openapis.org/) JSON for the project, so
be sure to include it - your API is your contract with the outside world so make
it as easy to discover and use as possible.

```C#
/// <summary>
/// My simple service.
/// </summary>
[WebApi("sample")]
public interface ISampleService
{
    /// <summary>
    /// Gets a simple greeting for the user.
    /// </summary>
    /// <returns>
    /// The traditional tutorial program response.
    /// </returns>
    [Get("greeting")]
    [Version(1)]
    Task<string> SimpleMessage();
}
```

The users of the service will be able to invoke the method by navigating to
`http://hostname/v1/sample/greeting` and since the class that implements the
interface has no dependencies on the HTTP context, it is easy to unit test the
logic without having to mock out the HTTP side of things.

Also note that the method is versioned - this allows for new methods to be
added that replace the old methods without breaking any third party code
written that's using the old method.

# Contributing

All contributions are welcome! Take a look at [CONTRIBUTING](CONTRIBUTING.md)
for some tips.

# Code of Conduct

This project has adopted the code of conduct defined by the
[Contributor Covenant](https://www.contributor-covenant.org/) to clarify
expected behaviour in our community. For more information see
[CODE_OF_CONDUCT](CODE_OF_CONDUCT.md).
