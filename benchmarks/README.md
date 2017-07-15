Benchmarks
==========

This folder contains some basic benchmarks for testing the internals of the
libraries. It isn't a substitution for good old fashions profiling. Once an
area has been identified that could be improved, create a simple benchmark of
the class/method and save the output of the testing (you only have to run your
benchmark class). Make some tweaks to the code and then re-run the benchmark,
comparing the new results to the old. Submit a pull request and make sure to
comment about the savings.

Replacing framework parts
-------------------------

If the code being changed is actually part of the .NET framework (e.g. changing
which method is called on a class or swapping a generic collection for a custom
one) then please create two benchmarks: one with the framework set as the
baseline and one with the new class/method. This makes comparisons easier and
lets us revisit areas later on when perhaps the .NET framework has been changed
to include an optimisation.

Profiling
---------

Try to always create the path being profiled in the `BasicExample` project so
that we have an example of how to use the library for others to explore. This
project should contain typical usage scenarios, therefore, the results of the
profiling should match a greater number of users (and therefore any
optimisations having a greater benefit to all).
