# SpeedyGenerators

![Nuget](https://img.shields.io/nuget/v/SpeedyGenerators)  ![Nuget](https://img.shields.io/nuget/dt/SpeedyGenerators)  ![GitHub tag (latest SemVer)](https://img.shields.io/github/v/tag/raffaeler/SpeedyGenerators)  ![GitHub release (latest by date)](https://img.shields.io/github/v/release/raffaeler/SpeedyGenerators) 

SpeedyGenerators is a collection of C# Code Generators (available since C# 9) which generate C# code to *augment* the code written by the developer.

### List of generators

* `MakePropertyAttribute` is applied to a field and generates (in the partial class) a property implementing the `INotifyPropertyChanged` interface.
* (new in version 1.1.0) `MakeConcreteAttribute` is applied to a partial class and generated (in the partial class) all the properties belonging to the interface specified in the attribute (full qualified name of the interface). The interface may or not be implemented by the class.

### How does this work

Apart from straightforward cases, the greatest majority of the code is generated using the Roslyn (C# compiler) API.

The attributes needed to trigger the generation are also generated. This means that:

* `SpeedyGenerators` is not a run-time dependency.
* `SpeedyGenerators` assembly binary is never needed in the deployment folder
* The code inside the `SpeedyGenerators` assembly is only invoked during the compilation process (which also happens inside the IDEs leveraging the *Analyzers* features)
* The attributes required trigger the code generation are always generated in the assembly referencing `SpeedyGenerators`. You can see the code using ILSpy or an equivalent tool.
  * A potential future breaking change is its namespace. The current namespace for the attribute is `SpeedyGenerators` but I am thinking to change it to the root namespace of the target process.


The generation process happens thanks to the a feature provided by the C# compiler. This means it works even if the build is done on the command line. This means that it will work in the CI process as well.

Once the package is referenced in the application, just start coding as shown in the examples. The generated code can be examined by expanding the Analyzers tree:

![image-20211026173825724](images/README/image-20211026173825724.png)

## `MakePropertyAttribute`

This (generated) attribute will trigger the implementation of the `INotifyPropertyChanged` interface.

The code you are expected to write in your class is the following:

- The `using` declaration needed to use the `MakeProperty` attribute.
- Making the class (in this case `FakeViewModel`) `partial`. If you forget it, the compiler will produce an error saying that another class exists with the same name but with the partial modifier.
- Write the private field (in this case `_x`) and tag it with the `MakeProperty` attribute
  - The only mandatory parameter of the attribute is the name of the property being generated.
- Every comment applied to the field will be copied to the generated property.

#### Code written from the developer:

```c#
using SpeedyGenerators;

namespace ConsoleTest_Extra
{
    internal partial class FakeViewModel
    {
        /// <summary>
        /// The comment is copied
        /// to the generated property
        /// </summary>
        [MakeProperty("X")]
        private int _x;
    }
}
```



#### Code generated as a partial class under the Analyzer tree:

```C#
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace ConsoleTest_Extra
{
    internal partial class FakeViewModel : INotifyPropertyChanged
    {
        
        /// <summary>
        /// Event triggered when a property changes its value
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
        /// <summary>
        /// The comment is copied
        /// to the generated property
        /// </summary>
        public int X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }
}
#nullable restore

```



### Optional parameters: ExtraNotify

The first optional parameter is `ExtraNotify`.

```C#
[MakeProperty("Name", true)]
private string? _name;
```

The following code is generated.

```C#
/// <summary>
/// </summary>
public string? Name
{
    get => _name;
    set
    {
        var oldValue = _name;
        _name = value;
        OnPropertyChanged();
        OnNameChanged(oldValue, _name);
    }
}

partial void OnNameChanged(string? oldValue, string? newValue);
```

This allows you to be notified inside the OnNameChanged by writing the following method below the field declaration:

```C#
partial void OnNameChanged(string? oldValue, string? newValue)
{
    Console.WriteLine($"{oldValue} -> {newValue}");
}
```



### Optional parameters: CompareValues

The second optional parameter is `CompareValues`.

```C#
[MakeProperty("Description", false, true)]
private string? _description;
```

The following code is generated.

```C#
/// <summary>
/// </summary>
public string? Description
{
    get => _description;
    set
    {
        if (_description == value)
            return;
        _description = value;
        OnPropertyChanged();
    }
}
```

The optional parameters can be, of course, combined together.

#### Final Notes

* The generated code supports adding the `using` declaration for types in other namespaces. For example, if the type is a `ObservableCollection`, the `using System.Collections.ObjectModel;` is added automatically.

* The original class may already have multiple partial classes. A single partial class will be generated.

* Two or more classes may have the same name but in different namespaces. In this case a progressive number is added to the file containing the generated code, as the C# code generators need a unique file name for the generated code.

* If the `INotifyPropertyChanged` interface is implemented in any of the base classes, the generator finds it and of course does NOT generate the event or even the `OnPropertyChanged` method.

  > Any event can only be invoked by a member of the same class and not from derived classes.

  In this case the generator examines the base class where the event is declared and looks for a method containing `PropertyChanged` in its name, with a single parameter of type string.

  If that is found, the generated code in the property setters will invoke that method.
  If that method is not found (because it has a different name), the generator will call anyway the **non-existent** `OnPropertyChanged` method. This method **is required** and must be manually written by the developer. The code inside this method should just call the method in the base class that can raise the event.



## `MakeConcreteAttribute`

> This Attribute is available starting from version 1.1.0

The `MakeConcreteAttribute` is meant to implement all the properties of an interface inside a class.

For example, let's say we have the following interface and we want to automatically implement those three properties in a class

```CSharp
namespace LibraryTest
{
    namespace Abc
    {
        interface IMyInterface
        {
            string Name { get; }
            StringBuilder Other { get; }
            int Age { get; }
        }
    }
}
```

The code the user would write is:

```CSharp
[MakeConcrete(
    interfaceFullTypeName: "LibraryTest.Abc.IMyInterface",
    generateInitializingConstructor: true,
    makeSettersPrivate: false,
    implementInterface: false,
    makeReferenceTypesNullable: true,
    makeValueTypesNullable: false)]
partial class MyClass
{
}
```

> Only 'interfaceFullTypeName' is a required argument, the others are optional

The generated code will be:

```CSharp
namespace LibraryTest
{
    partial class MyClass
    {
        public MyClass(string? name, StringBuilder? other, int age)
        {
            Name = name;
            Other = other;
            Age = age;
        }

        
        /// <summary>
        /// Implements IMyInterface.Name
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Implements IMyInterface.Other
        /// </summary>
        public StringBuilder? Other { get; set; }
        
        /// <summary>
        /// Implements IMyInterface.Age
        /// </summary>
        public int Age { get; set; }
    };
}
```

The attribute parameters are:

* `interfaceFullTypeName`. This string specify the full type of the interface to implement. This parameter is a string to avoid dependencies over the assembly containing the interface so that it can be dynamically loaded at runtime.
* `generateInitializingConstructor`. As for the example above, this will trigger the generation of the initialization constructor specifying all the properties defined in the interface.
* `makeSettersPrivate`. This specifies whether the properties should be private or public.
* `implementInterface`. When true, this parameter will make the partial class to derive the specified interface.
* `makeReferenceTypesNullable`. This specify whether or not the properties whose type is a **reference-type** should be implemented as nullable.
* `makeValueTypesNullable`.  This specify whether or not the properties whose type is a **value-type** should be implemented as nullable.



## Issues

Please use the Issues on the GitHub repository to provide bugs and suggestions.

