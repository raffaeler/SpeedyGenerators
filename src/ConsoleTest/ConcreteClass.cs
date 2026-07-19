using System;
using System.Collections.Generic;
using System.Text;

using SpeedyGenerators;

namespace ConsoleTest
{
    interface IMyInterface
    {
        string Name { get; }
        StringBuilder Other { get; }
        int Age { get; }
    }

    [MakeConcrete(
        interfaceFullTypeName: "ConsoleTest.IMyInterface",
        generateInitializingConstructor: true,
        makeSettersPrivate: false,
        implementInterface: false,
        makeReferenceTypesNullable: true,
        makeValueTypesNullable: false)]
    internal partial class ConcreteClass
    {
    }
}
