using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

namespace LibraryTest;

interface IMyInterface
{
    string Name { get; }
}

[MakeConcrete("LibraryTest.IMyInterface")]
partial class MyImplementation
{
}

partial struct MyStruct { }