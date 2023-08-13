# SpeedyGenerators

SpeedyGenerators is a collection of C# Code Generators (available since C# 9) which generate C# code to *augment* the code written by the developer.

### List of generators

* `MakePropertyAttribute` is applied to a field and generates (in the partial class) a property implementing the `INotifyPropertyChanged` interface.
* (new in version 1.1.0) `MakeConcreteAttribute` is applied to a partial class and generated (in the partial class) all the properties belonging to the interface specified in the attribute (full qualified name of the interface). The interface may or not be implemented by the class.

Please visit https://github.com/raffaeler/SpeedyGenerators for more details.



