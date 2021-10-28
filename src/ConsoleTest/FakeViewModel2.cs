using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OuterNs.InnerNs;

using SpeedyGenerators;

namespace ConsoleTest_Extra
{
    internal partial class FakeViewModel
    {
        [MakeProperty("X")]
        private int _x;

        [MakeProperty("Y")]
        private ObservableCollection<SomeType> _y = new();
    }
}


namespace OuterNs
{
    namespace InnerNs
    {
        public record SomeType(int A);
    }
}