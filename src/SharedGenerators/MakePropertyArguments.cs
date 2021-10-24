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

            var arg0 = arguments[0].Trim('\"');
            var arg1 = false;
            var arg2 = false;
            if (arguments.Length >= 2) bool.TryParse(arguments[1], out arg1);
            if (arguments.Length >= 3) bool.TryParse(arguments[2], out arg2);

            instance = new MakePropertyArguments(arg0, arg1, arg2);
            return true;
        }

        public string Name { get; private set; }
        public bool ExtraNotify { get; private set; }
        public bool CompareValues { get; private set; }
    }
}

