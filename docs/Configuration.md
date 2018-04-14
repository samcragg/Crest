# Configuration

To allow values to be configured at runtime, out of the box Crest supports
injecting simple data classes into your constructor that represent the
configuration options. To do this, simply mark the class representing the
options with the `ConfigurationAttribute` and in then ask for that class in your
constructor:

``` C#
[Configuration]
public class MyOptions
{
    public bool HasOption { get; set; }
}

public class MyLogic : IMyService
{
    public MyLogic(MyOptions options)
    {
        ...
    }
}
```

## Configuration Providers

Out of the box, the framework will initialize default values and look for the
`appsetting.json` file (using the one for the current
[environment](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments)
if it exists). To provide your own provider, simply implement the
`IConfigurationProvider` interface and it will get automatically picked up at
runtime.

## Default Values

To provide default values for properties if they are not set, use the built in
[`DefaultValueAttribute`](https://docs.microsoft.com/en-gb/dotnet/api/system.componentmodel.defaultvalueattribute)
on the property and this will be correctly used as a fallback value should none
be specified:

``` C#
[DefaultValue(true)]
public bool HasOption { get; set; }
```
