using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

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

    [MakeConcrete(
     interfaceFullTypeName: "LibraryTest.Abc.IMyInterface",
     generateInitializingConstructor: true,
     makeSettersPrivate: false,
     implementInterface: false,
     makeReferenceTypesNullable: true,
     makeValueTypesNullable: false)]
    partial record class MyRecord { }

    partial struct MyStruct { }

    partial record struct MyRecordStruct { }

}