# Crest

[![Build status](https://ci.appveyor.com/api/projects/status/spal08yea33stdlw/branch/master?svg=true)](https://ci.appveyor.com/project/samcragg/crest/branch/master) [![Coverage Status](https://coveralls.io/repos/github/samcragg/Crest/badge.svg?branch=master)](https://coveralls.io/github/samcragg/Crest?branch=master) ![License](https://img.shields.io/github/license/samcragg/crest.svg)

This library provides a simple way to create versioned RESTful microservices
built against .NET Core with ease that are performant.

The HTTP side of the requests/responses is abstracted away as much as possible,
so writing the logic code looks just like a traditional server application and
enables multiple microservices implemented with Crest to have a consistent
feel to them from the users perspective. It also provides a simple mechanism to
version the API, to allow for new functionality to be added without effecting
older clients.

# Basic usage

First create an interface that describes the routes. The XML documentation will
be converted to a [Swagger](http://swagger.io/) json for the project, so be
sure to include it in your contract with the outside world:

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
interface has no dependencies on the HTTP context, it is easy to unit test.
