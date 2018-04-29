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

## Application Settings

Values inside the `appsettings.json` file will automatically get injected after
the default values have been set. After this, any value inside the environment
specific application file (e.g. `appsettings.Production.json`) will be injected.
This means the environmental settings will override the global application
settings.

To map the values to the correct class, the typename should be specified as the
JSON key and the object value will be read into the configuration class. Any
value not specified will be left uninitialized (i.e. properties not specified in
the JSON object written to). If two types have the same name then you can use
the fully qualified type name (e.g. `Namespace.Class`) to differentiate them.
When matching the type (or the properties in the type), case is not taken into
account (i.e. comparison is case insensitive).

Here's an example highlighting the behaviour of the above.

`appsettings.json`

``` JSON
{
    "myConfig":{
        "setting1": "global 1",
        "setting2": "global 2"
    }
}
```

`appsettings.Production.json`

``` JSON
{
    "myConfig":{
        "setting2": "environment"
    }
}
```

`Code`

``` C#
[Configuration]
public class MyConfig
{
    // Will return "global 1"
    public string Setting1 { get; set; }

    // Will return "environment" when ASPNETCORE_ENVIRONMENT is "Production" -
    // otherwise will return "global 2" for other environments
    public string Setting2 { get; set; }
}
```
