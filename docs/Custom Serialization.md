# Custom Serialization

It is recommended that wherever possible the returned types on the API methods
be simple types that contain primitives and/or other simple types to maximise
the compatibility with consumers of the API. However, should you need to control
the way a type is read/written then you can use the following options.

## Ignoring properties

If you want to include a property on an object for internal use only, then you
can put the `Browsable(false)` attribute on it. Note that this is probably
indicating that there isn't a clean separation between the service and the logic
so should normally be avoided, preferring to use a dedicated data types for the
service and use mapping between the other classes (such as by using
[AutoMapper](http://automapper.org/)).

```C#
public class MyData
{
    public string VisibleProperty { get; set; }

    [Browsable(false)]
    public string InternalProperty { get; set; }
}
```

## Ordering properties

As the order of properties retrieved via reflection is not guaranteed,
properties are ordered by their name when writing them. To change the order, the
`DataMember` attribute can be applied to them specifying the order; properties
with their order specified will be written first in their respective order,
followed by any other properties ordered by their name.

```C#
public class MyData
{
    public string Bottom { get; set; }

    [DataMember(order = 2)]
    public string Middle { get; set; }

    [DataMember(order = 1)]
    public string Top { get; set; }
}
```

The order for the above would be: `Top`, `Middle` and then `Bottom`.

## Custom serializers

If you need fine grained control of the serialized/deserialized data then you
can create a custom serializer for the type by implementing the `ISerializer`
interface:

```C#
public class MySerializer : ISerializer<MyData>
{
    public MyData Read(IClassReader reader)
    {
        ...
    }

    public void Write(IClassWriter writer, MyData instance)
    {
        ...
    }
}
```

With the `IClassReader` and `IClassWriter` you have more control over how data
is handled, however, they are relatively abstract to allow the same serialzer to
be used for different types (e.g. JSON and XML). If you would like to fall back
on the default serialization for a type then you can inject the serializer for
that type in the constructor:

```C#
    private readonly ISerializer<NestedData> serializer;

    public MySerializer(ISerializer<NestedData> serializer)
    {
        this.serializer = serializer;
    }
```

This should allow you to mix custom serialization with the normal serialization
for some of the properties.
