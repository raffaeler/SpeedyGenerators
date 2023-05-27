using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedyGenerators
{
    internal class MakePropertyArguments
    {
        public MakePropertyArguments(string name, bool extraNotify, bool compareValues)
        {
            Name = name;
            ExtraNotify = extraNotify;
            CompareValues = compareValues;
        }

        public static bool TryParse(string[]? arguments,
            out MakePropertyArguments? instance)
        {
            if (arguments == null || arguments.Length == 0)
            {
                instance = null;
                return false;
            }

            var arg0 = Utilities.ExtractString(arguments, 0, string.Empty);
            var arg1 = Utilities.ExtractBoolean(arguments, 1, false);
            var arg2 = Utilities.ExtractBoolean(arguments, 2, false);

            instance = new MakePropertyArguments(arg0, arg1, arg2);
            return true;
        }

        public string Name { get; private set; }
        public bool ExtraNotify { get; private set; }
        public bool CompareValues { get; private set; }
    }
}

