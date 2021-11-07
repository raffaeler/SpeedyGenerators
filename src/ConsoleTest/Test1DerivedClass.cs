using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;
namespace ConsoleTest
{
    internal partial class Test1DerivedClass : Test1BaseClass
    {
        [MakeProperty("Y")]
        private int _y;

    }
}
