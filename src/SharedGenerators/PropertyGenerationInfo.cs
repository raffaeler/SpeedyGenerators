using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators;

internal class PropertyGenerationInfo
{
    public PropertyGenerationInfo(string propertyName, TypeSyntax propertyType)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    public string PropertyName { get; }
    public TypeSyntax PropertyType { get; }
    public HashSet<string> PropertyTypeNamespaces { get; set; } = new HashSet<string>();
}
