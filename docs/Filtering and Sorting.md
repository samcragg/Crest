# Filtering and Sorting

By installing the `Crest.DataAccess` package, you can easily enable dynamic
filtering and/or sorting of your data based on the query parameters.

## Filtering

Filtering is performed by specifying the property to filter as the key and the
value to filter it by as the value. The value to filter by can be in the form of
`operator:value` - if no operator is specified then `eq` will be used. Note if
you need to use the `:` character in the value to filter by, then simply specify
the operator as only the first `:` is used as a separator (i.e. `name=eq:a:b`
would search for names equal to `a:b`).

| Operator   | Description                                               |
|------------|-----------------------------------------------------------|
| eq         | Matches values equal to the value. *This is the default.* |
| ne         | Matches values not equal to the value.                    |
| in         | Matches values that appear in the list.                   |
| gt         | Matches numbers greater than the value.                   |
| ge         | Matches numbers greater than or equal to the value.       |
| lt         | Matches numbers less than the value.                      |
| le         | Matches numbers less than or equal to the value.          |
| contains   | Matches text that contains the value.                     |
| endswith   | Matches text that ends with the value.                    |
| startswith | Matches text that starts with the value.                  |

Note that the value for `in` should be a comma separated value (as per
[RFC 4180](https://tools.ietf.org/html/rfc4180)), for example, `name=in:a,b,c`.

## Sorting

Sorting is performed by using the `sort` query parameter and listing the
properties to sort by, optionally with their sort direction (separated by a
`:`). Multiple properties can be separated with a comma (e.g.
`sort=name,age:desc`) or by specifying the query multiple times (e.g.
`sort=name&sort=age:desc`). If no sort direction is specified then the property
values will be sorted in ascending order.

| Operator | Description                                   |
|----------|-----------------------------------------------|
| asc      | Sort the property values in ascending order.  |
| desc     | Sort the property values in descending order. |

The order or sorting will be based on the order the properties are specified,
i.e. `sort=name,age` will sort by `name` and then a subsequent ordering by `age`
where multiple `name` values are equal.

## Usage

The filtering and sorting can be applied on any
[`IQueryable<T>`](https://docs.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1)
using the `Apply` extension method:

```C#
using Crest.DataAccess;

...

public Task<MyObject[]> FetchObjects(dynamic query)
{
    IQueryable<MyObject> result = CreateDatabaseQuery();

    result = result.Apply().FilterAndSort(query);

    return result.ToArrayAsync();
}
```

There are options to either filter the results, to sort them or to perform both
(as per the above example). You can also specify the type of the object to use
as the source of the filtered/sorted properties (see [Mapping](#mapping) below
for how to specify the association between the properties):

```C#
    IQueryable<DataObject> result = ...
    result = result.Apply().FilterAndSort<ServiceObject>(query);
```

This technique can also be used to limit which fields can be filtered/sorted
(for example, to limit the filtering on columns in the database that are
indexed) by having an internal type that only contains the allowed fields.

## Mapping

Typically in a layered architecture design, the objects exposed via a service
are different to the objects used to access the data store.​ To allow the API
consumers to use the service objects to specify filtering you can tell the
framework how to map between those objects and your data objects. To do this
create a class that implements the `IMappingInfoFactory` interface. This
interface is used to create `MappingInfo` classes, that contain the source and
destination types along with an
[Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression)
that has the assignments between the two types.

To make building these mapping easier, there is the `MappingInfoBuilder` class
that can be used to create the `MappingInfo`:

```C#
public ExampleMapper : IMappingInfoFactory
{
    public IEnumerable<MappingInfo> GetMappingInformation()
    {
        var builder = new MappingInfoBuilder<ServiceType, DataType>();
        builder.Map(s => s.Name).To(d => d.FullName);
        yield return builder.ToMappingInfo();
    }
}
```

If you are using [AutoMapper](https://automapper.org/), you can use the
[BuildExecutionPlan](https://automapper.readthedocs.io/en/latest/Understanding-your-mapping.html)
method to generate the `Expression` for you, resulting in minimal code for the
interface (note you will probably need to use the
[ReverseMap](https://automapper.readthedocs.io/en/latest/Reverse-Mapping-and-Unflattening.html)
method when setting up your mappings as normally you map from the data type to
service type but we need to go the other way around when filtering):

```C#
public class WithAutoMapper : IMappingInfoFactory
{
    public IEnumerable<MappingInfo> GetMappingInformation()
    {
        yield return CreateMapping<ServiceType, DataType>();
    }

    private static MappingInfo CreateMappingInfo<TService, TData>()
    {
        Expression mapping = Mapper.Configuration.BuildExecutionPlan(
            typeof(TService),
            typeof(TData));

        return new MappingInfo(typeof(TService), typeof(TData), mapping);
    }
}
```
