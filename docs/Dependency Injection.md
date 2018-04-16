# Dependency Injection

The library is designed to out of the box provide the basic infrastructure
required to write testable and maintainable RESTful APIs. To support this, there
is built in support for dependency injection that should just work for the
majority of scenarios:

```C#
public interface IMyService
{
    ...
}

public class MyService : IMyService
{
    ...
}

public class MyLogic
{
    public MyLogic(IMyService service)
    {
        // service will be an instance of MyService at runtime
    }
}
```

## Registration

At start-up, the assemblies are scanned and concrete classes are automatically
registered to the interfaces they implement. This works for both public and
internal types and means there is no need for explicit mapping.

If there is custom work you really need to do, then you have the option to
implement your own `IServiceRegister` or simply inherit from the existing
`ServiceLocator` implementation that has the necessary extension points for
registration and resolving of services.

## Resolving Services

To get a service resolved, simply specify it in the constructor; property
service injection is not supported. If there are multiple services, then you can
either specify an `IEnumerable<T>` or `T[]` and all services implementing the
specified type will be injected. This can also be used for discovering optional
services, in this case an empty sequence/array will be passed in.

If a type has multiple constructors, then the constructor with the most amount
of parameters that can be resolved will be used.

## Lifetime

All services are created as transient per request and are automatically disposed
(if they implement `IDisposable`) when the request is finished. This is to
reduce the need to write thread safe code, as instance methods/fields will only
be accessed on a single thread.

## Internal Details

Dependency injection is provided by the wonderful (and fast!)
[DryIoc](https://bitbucket.org/dadhi/dryioc), though this is is all kept
internal so there's no need to learn anything about it's use.
