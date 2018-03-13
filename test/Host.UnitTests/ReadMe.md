Important Information
=====================

Because some of the classes rely on using `ArrayPools`, the unit tests make use
of [Costura](https://github.com/Fody/Costura) to ensure that the static
properties are switched out before any test is ran. See the `ModuleInitializer`
class for the code that is ran once before any test is ran.
