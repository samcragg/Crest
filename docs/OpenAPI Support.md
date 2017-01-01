# OpenAPI Support

The framework supports the generation of [OpenAPI](https://www.openapis.org/)
JSON files at compile time, which can then be used as part of a static webpage
or can be served by the micro service via an additional package. The information
for the documentation is taken from the routing attributes and XML documentation
on the interfaces, so this shouldn't be too much of a burden for developers.

# OpenAPI Document Information

The generated file will follow the [version 2.0](https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md))
specification. The information (i.e. `infoObject`) will contain the details from
the following assembly attributes:

    [assembly: AssemblyTitle("Title")]
    [assembly: AssemblyDescription("Description")]
    [assembly: AssemblyMetadata("License", "License name")]
    [assembly: AssemblyMetadata("LicenseUrl", "http://www.example.com")]

Although none of the attributes are required, if the assembly title attribute is
not specified, the name of the assembly will be used. Also, if the license URL
is specified then the license must also be there but you can just specify the
license without the URL.

# Tags

When using the framework all routes are defined inside interfaces, allowing
routes to be naturally grouped together by functionality. To group the paths
together in the documentation, all methods defined in an interface are tagged
with the same tag, allowing them to appear grouped together in the generated
documentation. The name and description of the tag comes from the interface name
(minus the `I` prefix) and summary XML documentation.

Alternatively, the `Description` attribute can be applied to the interface to
customise the name of the generated tag (note the
[`DisplayName`](https://msdn.microsoft.com/en-us/library/system.componentmodel.displaynameattribute.aspx)
attribute isn't allowed on interfaces, hence the slightly obscure use of this one):

    /// <summary>
    /// Description of the interface.
    /// </summary>
    [Description("Products")]
    public interface IUserProducts
    {
    }

This would generate something looking like this (note if the `Description`
attribute was not specified, the tag would be named `UserProducts`):

![Example tag output](images/TagExampleOutput.png)
