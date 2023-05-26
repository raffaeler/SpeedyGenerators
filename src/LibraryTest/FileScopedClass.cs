using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

namespace LibraryTest;

internal partial class ClassOutsideNamespace
{
    [MakeProperty("MyProperty")]
    private int _myField1;
}

file partial class FileScopedClass
{
    // Cannot use source generators with file scoped classes because
    // - source generators must create a separate source file
    // - file scoped classes must reside in the same file
    //[MakeProperty("YourProperty")]
    //protected int _yourField1;
}

// file scoped partial classes can be defined only in the same file
file partial class FileSCopedClass
{
}