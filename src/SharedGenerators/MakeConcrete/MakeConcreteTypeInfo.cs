using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators;

internal class MakeConcreteTypeInfo
{
    public MakeConcreteTypeInfo(ConcreteTypeKind concreteTypeKind,
        BaseTypeDeclarationSyntax typeDeclaration,
        string namespaceName,
        string typeName,
        MakeConcreteArguments arguments)
    {
        ConcreteTypeKind = concreteTypeKind;
        TypeDeclaration = typeDeclaration;
        NamespaceName = namespaceName;
        TypeName = typeName;
        AttributeArguments = arguments;
        FullName = $"{NamespaceName}.{TypeName}";
    }

    public ConcreteTypeKind ConcreteTypeKind { get; private set; }
    public BaseTypeDeclarationSyntax TypeDeclaration { get; private set; }
    public string NamespaceName { get; private set; }
    public string TypeName { get; private set; }
    public string MockingTypeName { get; set; } = string.Empty;
    public MakeConcreteArguments AttributeArguments { get; }
    public string FullName { get; private set; }
    public List<string> FullNameBaseTypes { get; } = new();
    public List<PropertyGenerationInfo> Properties = new();
    public HashSet<string> Namespaces { get; set; } = new HashSet<string>();

    public override string ToString()
    {
        return $"{FullName} ";
    }
}
