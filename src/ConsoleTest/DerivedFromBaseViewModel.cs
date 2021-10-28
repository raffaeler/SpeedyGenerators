using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;
namespace ConsoleTest
{
    internal partial class DerivedFromBaseViewModel : BaseViewModel
    {
        [MakeProperty("Abc")]
        private int _abc;


    }
}
