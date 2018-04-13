# Serializer Class Generation

> **Internal documentation**
>
> This documents implementation details and is not required for normal usage

When scanning the methods for routing information, when a route is found a
dynamic class is created for serializing the return type to the response
stream. This happens once at startup and the generated class type can be used
for all requests. Out of the box there are serializers for JSON and XML.

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
as `int`, `decimal`, `DateTime`. As the results of methods gets passed to the
serialization pipeline as an object, there's no performance gain using structs
so this rule is designed to ensure consistency and better documentation.

Only public properties that have both a public get and set are serialized. If
a property has `Browsable(false)` on it then it will also be excluded, but this
should really be avoided as the class is probably being used for two purposes.
Any property that has a null value will not be emitted in the final output.

The order of the properties matches the declared order in the class unless a
property has the `DataMember` attribute applied to it with the `Order` set to
a value, which take precedence over non-ordered properties (i.e. will be
serialized before them). Again, this should be avoided but is provided to
enable scenarios where the order is important due to backwards compatibility.

## Custom Serializers

A custom serializer can be created using the following template:

```C#
public class MySerializer : IClassSerializer<MyMetadataType>
{
    protected MySerializer(Stream stream, SerializationMode mode)
    {
        ...
    }

    protected MySerializer(MySerializer parent)
    {
        ...
    }

    public static bool OutputEnumNames { get; }

    public static MyMetadataType GetMetadata(PropertyInfo property)
    {
        ...
    }

    // Optional: If the serializer need extra metadata passing in the BeginClass
    // method then implement this, otherwise, the default value for the metadata
    // type will be passed in
    public static string GetTypeMetadata(Type type)
    {
        ...
    }

    // IClassSerializer methods
}
```

Things to note. The class MUST be public, not sealed and have the two
constructors, as this class will be used as the base class for each generated
class. The class also must have the above public static methods, which are
called during the generation of the derived class.

The constructor taking a `Stream` is called when the class is being used to
serialize the root object, with the `Stream` representing the response stream.
The second constructor is called for nested types (i.e. when the serializer
is created by another serializer).

## Generated Classes

To help understand how the base class will get used, lets take a look at how a
serializer will be generated for this simple data class:

```C#
public sealed class DataClass
{
    public int Number { get; set; }

    public string Text { get; set; }
}
```

Given the above, the generated class will have the same form as this C# code:

```C#
public sealed class MySerializer<>DataClass : MySerializer, ITypeSerializer
{
    public static MyMetadataType Number_MetaData;
    public static MyMetadataType Text_MetaData;
    public static MyMetadataType DataClass_MetaData; // Optional

    public void Write(object value)
    {
        DataClass instance = (DataClass)value;
        this.WriteBeginClass(DataClass_MetaData);

        this.WriteBeginProperty(Number_MetaData);
        this.Writer.WriteInt32(instance.Number);
        this.WriteEndProperty();

        if (instance.Text != null)
        {
            this.WriteBeginProperty(Text_MetaData);
            this.Writer.WriteString(instance.Text);
            this.WriteEndProperty();
        }

        this.WriteEndClass();
    }

    public void WriteArray(Array value)
    {
        DataClass[] array = (DataClass[])value;
        this.WriteBeginArray(typeof(DataClass), array.Length);

        if (array.Length > 0)
        {
            if (array[0] != null)
            {
                this.Write(array[0]);
            }
            else
            {
                this.WriteNull();
            }

            for (int i = 1; i < array.Length; i++)
            {
                this.WriteElementSeparator();
                if (array[i] != null)
                {
                    this.Write(array[i]);
                }
                else
                {
                    this.WriteNull();
                }
            }
        }

        this.WriteEndArray();
    }
}
```

As can be seen, the generated class calls the base methods for each of the
properties and also performs `null` checking for reference/nullable values. It
also calls the specialized method of the `IStreamWriter` for serializing
primitive types, falling back to the generic `WriteObject` method if none are
available.

If the class contains a property that is another class then the serializer for
that type will be used. This is done by having a field in the generated class
that is initialized during construction, i.e.

```C#
public sealed class MySerializer<>OtherDataClass : MySerializer, ITypeSerializer
{
    // Metadata fields
    ...
    private readonly MySerializer<>DataClass dataClassSerializer;

    public MySerializer<>OtherDataClass(Stream stream)
        : base(stream)
    {
        this.dataClassSerializer =
            new MySerializer<>DataClass(this.StreamWriter);
    }

    public void Write(object value)
    {
        ...
        if (instance.DataClassProperty != null)
        {
            this.WriteBeginProperty(DataClassProperty_MetaData);
            this.dataClassSerializer.Write(instance.DataClassProperty);
            this.WriteEndProperty(DataClassProperty_MetaData);
        }
        ...
    }
}
```

## Primitive Types

If just writing a primitive type to the stream (i.e. the method returns an int
or string etc) then a different generated serializer is used. These are all
generated at startup for all the primitive types (which are the types accepted
by the various `WriteXXX` methods in the `IStreamWriter` interface).

```C#
public sealed class MySerializer<>Int32 : MySerializer, ITypeSerializer
{
    public MySerializer<>Int32(Stream stream) : base(stream) { }

    public void Write(object instance)
    {
        this.BeginWrite(null); // Since there's no GetTypeMetadata, null will be passed in
        this.Writer.WriteInt32((int)instance);
        this.EndWrite();
    }

    public void WriteArray(Array value)
    {
        int[] array = (int[])value;
        this.WriteBeginArray(typeof(int), array.Length);

        if (array.Length > 0)
        {
            this.Writer.WriteInt32(array[0]);
            for (int i = 1; i < array.Length; i++)
            {
                this.WriteElementSeparator();
                this.Writer.WriteInt32(array[i]);
            }
        }

        this.WriteEndArray(typeof(int));
    }
}
```
