Errors
======

The following are reported as errors by the analyzer.


MustReturnTask
--------------

Since all route handlers are invoked asynchronously, the return type must be
either a `Task`, if no data is to be returned to the client, or `Task<T>`. The
following would trigger the error as it is not returning a `Task` derived type:

    [Get(...), Version(...)]
    string Method();

A code fix is available that will change `void` methods to `Task` and wrap the
return type of other methods in the generic `Task<T>`. Applying the fix to the
above would produce this:

    [Get(...), Version(...)]
    Task<string> Method();


Warnings
========

The following are warnings that probably indicate that a convention is not
being followed etc.


ParameterPluralization
----------------------

To help third party developer discover the API, if multiple values can be
specified for a parameter then the parameter name should be plural. Likewise,
if only a single value can be specified it should be singular.

This warning is generated when either an array type has a singular name or a
non-array type has a plural name. Note only arrays are considered -
`IEnumerable<T>`, `List<T>` etc are not supported by the analyzer or by the
Crest framework.

    [Get("/things/{id}"), Version(...)]
    Task Method(Guid[] id);

Here there are two code fixes available, one will change the name (including
any parameter capture in the route) and the other will change the type.
Applying them to the above would produce the following:

    // Change name
    [Get("/things/{ids}"), Version(...)]
    Task Method(Guid[] ids);

    // Change type
    [Get("/things/{id}"), Version(...)]
    Task Method(Guid id);
