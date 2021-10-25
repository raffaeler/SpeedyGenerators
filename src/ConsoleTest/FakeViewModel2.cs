using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

namespace ConsoleTest_Extra
{
    internal partial class FakeViewModel
    {
        [MakeProperty("X")]
        private int _x;
    }
}
