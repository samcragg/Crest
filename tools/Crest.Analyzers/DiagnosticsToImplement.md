Errors
======

The following are reported as errors by the analyzer.


InvalidCaptureSyntax
--------------------

The analyzer was unable to parse part of the route. The syntax for a parameter
capture in the route is `{ + parameterName + }`. For example, the following
would trigger the error as the end bracket is missing:

    [Get("/things/{capture")]



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


ParameterDoesNotExist
---------------------

A parameter capture has been specified in the route but no parameter was found
with the same name. Note, as with C#, the name of the parameter in the capture
is case sensitive.

    [Get("/things/{id}"), Version(...)]
    Task Method();

Since the method does not have any parameters, the above triggers the error. A
code fix is provided that will add the parameter, so applying it to the above
would produce:

    [Get("/things/{id}"), Version(...)]
    Task Method(string id);


ParameterNotSpecifed
--------------------

This occurs when a method has a routing attribute applied to it but not all
the parameters have been specified in the route/body. For example, here only
one of the parameters is specified in the route:

    [Get("/things/{id}"), Version(...)]
    Task Method(Guid id, string name);


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
