# Links

Part of creating a flexible API is to allow clients to access further
information about a resource. To aid this, Crest supports building links that
can be returned to the client in a type safe manner.

## Link

If you wish to return a link to a resource that is in the HAL convention then
you can return a `Link`. For simple links, this can be created directly or, for
more customization, can be created using the `LinkBuilder`.

If you want to return multiple links, then you can use the `LinkCollection`, as
this allow you to specify the relationship of the link to the object you are
returning. To provide consistency for the clients, there is also the `Relations`
class that has some predefined constants for common types of links returned.

## Automatic links

To make it easier to link to other methods in the service, you can use the
`ILinkProvider` interface to invoke a method and have that converted to a link
that the client can call. For example, given the following service:

```C#
public interface IExampleService
{
    [Get("/frob/{id}")]
    [Version(1)]
    Task<string> GetFrobDetails(int id);
}
```

If we wanted to give the client a link they could use to fetch the details of a
frob with id 123, we could use the following code:

```C#
// This assumes that linkProvider is a type of ILinkProvider and has been
// injected into the constructor
Link link = this.linkProvider
    .LinkTo<IExampleService>(Relations.Next)
    .Calling(x => x.GetFrobDetails(123))
    .Build();
```

This would produce a link with the following properties:

```text
HRef: /v1/frob/123
RelationType: next
```

Note that the return value from `Calling` is a `LinkBuilder`, so you are free to
change other properties of the link before calling `Build`. Also, the version in
the URL will be the minimum version that is supported by the end point. It's
worth pointing out that the values passed in to the service method don't
have to be constant values, however, if they are a variable then their value
will be evaluated at the point of invoking `Calling`, i.e. the following works
as expected (demonstrates the capturing of `this` and the `id` parameter):

```C#
.Calling(x => x.GetFrobDetails(this.FindTheNextId(id)))
```

Here `FindTheNextId` would be invoked before the invocation of `Calling` has
returned.
