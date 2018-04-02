# Request Plugins

Similar to how you can add middleware to the
[ASP .NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware)
pipeline, if you need to perform custom logic either before or after each
request, you can create a class that implements the `IPreRequestPlugin` or the
`IPostRequestPlugin` interface, respectively.

They are invoked serially one after the other according to the values in the
`Order` property, with lower values executing first.

## Dependency Injection

Since the plugins are created for each request, they can have their dependencies
passed in to the constructor in the normal fashion and are not required to be
thread safe (i.e. they will only be used for a single request).

Should a plugin need to make data available for later processing (either within
another plugin that is invoked after this one or for the class that the request
gets routed to), you can inject an `IScopedServiceRegister` into the constructor
that can be used to register transient services (i.e. objects that live as long
as the request):

``` C#
public sealed class MyPlugin : IPreRequestPlugin
{
    private readonly IScopedServiceRegister serviceRegister;

    public MyPlugin(IScopedServiceRegister register)
    {
        this.serviceRegister = register;
    }

    public int Order => 100;

    public Task<IResponseData> ProcessAsync(IRequestData request)
    {
        this.serviceRegister.UseInstance(typeof(IMyService), new MyService());
        ...
    }
}
```

In this example, any class wanting an `IMyService` will get the created instance
injected into their constructor and, if `MyService` implements `IDisposable`, it
will automatically be disposed of at the end of the request.

### Passing Services Between Plugins

In the previous example, if the service needs to be injected into another plugin
then you will need to inject in a `Lazy<T>` version of it. This is because all
the plugins are created at the same time (as we need to sort their order),
therefore, any newly registered services won't be available yet.

``` C#
public sealed class SecondPlugin : IPreRequestPlugin
{
    private readonly Lazy<IMyService> myService;

    public SecondPlugin(Lazy<IMyService> myService)
    {
        // Do NOT access this yet, just store it for later
        this.myService = myService;
    }

    public int Order => 200;

    public Task<IResponseData> ProcessAsync(IRequestData request)
    {
        ...
    }
}
```

As this plugin is will be processed after `MyPlugin` (due to the values in
`Order`), the value of the `myService.Value` (when accessed in the
`ProcessAsync` method) will now contain the instance registered by `MyPlugin`.
