# Health Page

The health page shows information about the current machine running the service,
including CPU and memory usage of the application. It also lists out all the
loaded assemblies and their version. To access the page, navigate to
`hostname/health` (note there is no version).

The health page can be used to quickly diagnose if the service is running and
also to help identify problems where routes aren't being picked up, as it shows
the loaded assemblies.

## Enabling The Health Page

By default, the health page is enabled for
[Development environments](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments)
only, as it contains information about the machine running the service. If you
want to change this behaviour then you can set this option in the
`appsettings.json`:

```JSON
{
    "healthPageOptions": {
        "environments": [ "Development" ]
    }
}
```

The `environments` setting is an array of strings; to disable the health page
completely simply set this to an empty array.
