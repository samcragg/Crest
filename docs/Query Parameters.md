Query Parameters
================

At the moment, parameters being captured by query values have to be optional.
This is because we can't reject a request because part of the query is missing;
it's up to the application logic to validate the arguments if that behaviour is
desired.

However, we could support overloading depending on the query keys sent. For
example, if we had the following (to be clear, _the following isn't supported_):

    [Get("/customers?dob={dateOfBirth}")]
    FindByDateOfBirth(DateTime dateOfBirth = default(DateTime))

    [Get("/customers?gn={givenName}&fn={familyName}")]
    FindByName(string givenName = null, string familyName = null)

Because the query keys are optional, if a request simply sent the given name
(e.g. `.../person?gn=Sam`) then we could invoke the `FindByName` method as it
doesn't need both of its arguments. However, since we don't validate the query
key/values, it's unknown what to do if we received a request that specified the
date of birth and the family name. In this case, we'd have to introduce a
priority of the captures (we can't rely on their order as the key/values can be
specified in any order and multiple times), which adds complexity and introduces
extra learning on developers.

So at the moment the framework does not allow for overloaded by query value
combinations - this is something that could be explored but the use cases would
need to be clearly documented to avoid developers falling out of the pit of
success. The framework does, however, allow an overload of the route to include
a query part or not, i.e. this is currently allowed:

    [Get("/customers")]
    GetAll()

    [Get("/customers?gn={givenName}&fn={familyName}&dob={dateOfBirth}")]
    Filter(string givenName = null, string familyName = null, DateTime dateOfBirth = default(DateTime))

This burdens the validation of what argument combinations onto the API
developer, however, this does mean that they can unit test the valid
combinations and expand on what's valid without effecting their public API.
