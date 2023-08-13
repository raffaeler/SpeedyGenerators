using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedyGenerators;

internal class MakeConcreteArguments
{
    public MakeConcreteArguments(string mockingFullTypeName,
        bool generateInitializingConstructor,
        bool makeSettersPrivate,
        bool implementInterface,
        bool makeReferenceTypesNullable,
        bool makeValueTypesNullable)
    {
        MockingFullTypeName = mockingFullTypeName;
        GenerateInitializingConstructor = generateInitializingConstructor;
        MakeSettersPrivate = makeSettersPrivate;
        ImplementInterface = implementInterface;
        MakeReferenceTypesNullable = makeReferenceTypesNullable;
        MakeValueTypesNullable = makeValueTypesNullable;
    }

    public static bool TryParse(string[]? arguments,
        out MakeConcreteArguments? instance)
    {
        if (arguments == null || arguments.Length == 0)
        {
            instance = null;
            return false;
        }

        var arg0 = Utilities.ExtractString(arguments, 0, string.Empty);
        var arg1 = Utilities.ExtractBoolean(arguments, 1, true);        // this is the default in the generated attribute
        var arg2 = Utilities.ExtractBoolean(arguments, 2, false);
        var arg3 = Utilities.ExtractBoolean(arguments, 3, false);
        var arg4 = Utilities.ExtractBoolean(arguments, 4, false);
        var arg5 = Utilities.ExtractBoolean(arguments, 5, false);

        instance = new MakeConcreteArguments(arg0, arg1, arg2, arg3, arg4, arg5);
        return true;
    }

    public string MockingFullTypeName { get; private set; }
    public bool GenerateInitializingConstructor { get; }
    public bool MakeSettersPrivate { get; }
    public bool ImplementInterface { get; }
    public bool MakeReferenceTypesNullable { get; }
    public bool MakeValueTypesNullable { get; }

}

