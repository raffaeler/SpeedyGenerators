﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

namespace ConsoleTest
{
    internal partial class IntermediateViewModel : BaseViewModel
    {
        [MakeProperty("Something", true)]
        private int _something;

    }
}
