# Serializer Class Generation

> **Internal documentation**
>
> This documents implementation details and is not required for normal usage

When scanning the methods for routing information, when a route is found a
dynamic delegate is created for serializing the return type to the response
stream. This happens once at startup and the generated delegate can be used
for all requests. Out of the box there are serializers for JSON, XML and URL
encoded values.

## Rationale

In addition to performance improvements, the main motivating rationale for
implementing the serialization logic in this manor is to provide a consistent
way of serializing types and to encourage the types being returned are plain
old data types - most serialization frameworks have attributes that let you
tweak the output, which can be used to adapt a class that contains some
properties that are only relevant to the business logic so the class can be
re-used as the object to return from the service layer (for example, there
might be a temptation to have a property on the class which is used by the data
access layer to locate additional information).

The practice that the library tries to encourage is the returning of simple
data objects from the service layer that are well documented and language
agnostic. Therefore, using attributes to modify how the properties are returned
often leads to the documentation being out of date and also there being special
cases for one content format in comparison to another (e.g. an `XmlIgnore`
attribute can be placed on a member to stop it being in the XML output, but
this property may be present in other formats, making adding additional
supported content types more difficult).

## Class Serialization

Only classes can be serialized, with the exception of build in primitives such
as `int`, `DateTime` etc. As the results of methods gets passed to the
serialization pipeline as an object, there's no performance gain using structs
so this rule is designed to ensure consistency and better documentation.

Only public properties that have both a public get and set are serialized. If
a property has `Browsable(false)` on it then it will also be excluded, but this
should really be avoided as the class is probably being used for two purposes.
Any property that has a null value will not be emitted in the final output.

The order of the properties is alphabetical, as the order when reflecting over
the properties of a class is no guaranteed to be consistent. Properties with
the `DataMember` attribute applied to them with the `Order` set to
a value will take precedence over non-ordered properties (i.e. will be
serialized before them). Again, this should be avoided but is provided to
enable scenarios where the order is important due to backwards compatibility.

## Custom Formats

To enable the serialization of new formats, you'll need to create a class that
implements the `IFormatter` interface (the one inside the `Crest` namespace),
provides a static method for getting the metadata and has a constructor that
accepts a `Stream` and `SerializationMode`:

```C#
public class MySerializer : IFormatter
{
    public MySerializer(Stream stream, SerializationMode mode)
    {
        ...
    }

    public static object GetMetadata(PropertyInfo property)
    {
        ...
    }

    // Optional: If the serializer needs extra metadata passing in the BeginClass
    // method then implement this, otherwise, null will be passed in.
    public static object GetTypeMetadata(Type type)
    {
        ...
    }

    // IFormatter methods
}
```

You'll notice that there are some methods that accept a `string` and others that
accept an `object`. The `string` ones are called by custom serializers, whereas
the ones with `object` are called by the custom delegates, with the value passed
in being the return from `GetMatadata`. This provides an optimisation where you
can pre-calculate some data for a property/class, for example, you may escape
the name of the property so that there are no illegal characters - since the
property wont change it's a micro-optimisation to do the work once and then
reuse the value each time the class is serialized.

## Generated Delegates

There are two generated delegate types, which have the following signatures:

```C#
/// <summary>
/// Deserializes a class from the formatter.
/// </summary>
/// <param name="formatter">Used to read the data stream.</param>
/// <param name="metadata">Contains the pre-generated metadata.</param>
/// <returns>The deserialized value.</returns>
internal delegate object DeserializeInstance(IFormatter formatter, IReadOnlyList<object> metadata);

/// <summary>
/// Serializes a class to the formatter.
/// </summary>
/// <param name="formatter">Used to write the data stream.</param>
/// <param name="metadata">Contains the pre-generated metadata.</param>
/// <param name="instance">The instance to serialize.</param>
internal delegate void SerializeInstance(IFormatter formatter, IReadOnlyList<object> metadata, object instance);
```

As can be seen, all the metadata for a serializer is stored in an array and
passed in to the delegate, which will pass the correct value in to the methods.
The formatter is also passed in, which means a single delegate is generated for
the class and can be reused for different formats.

To see the generated code, a great place to look with the debugger is inside
`DelegateBuilder.BuildBody`, where the lambda is put together (the `DebugView`
displays the expression quite nicely).
