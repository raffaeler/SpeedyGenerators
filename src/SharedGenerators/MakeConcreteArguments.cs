using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedyGenerators;

internal class MakeConcreteArguments
{
    public MakeConcreteArguments(string interfaceFullTypeName, bool implementInterface, bool makeSettersPrivate)
    {
        InterfaceFullTypeName = interfaceFullTypeName;
        ImplementInterface = implementInterface;
        MakeSettersPrivate = makeSettersPrivate;
    }

    public static bool TryParse(string[]? arguments,
        out MakeConcreteArguments? instance)
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

        instance = new MakeConcreteArguments(arg0, arg1, arg2);
        return true;
    }

    public string InterfaceFullTypeName { get; private set; }
    public bool ImplementInterface { get; private set; }
    public bool MakeSettersPrivate { get; private set; }

}
