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
        }
    }

    [MakeConcrete("LibraryTest.Abc.IMyInterface")]
    partial class MyImplementation
    {
    }

    partial struct MyStruct { }

}