using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators;

internal class MakePropertyFieldInfo
{
    public MakePropertyFieldInfo(string fieldName, TypeSyntax fieldType,
        string[] comments, MakePropertyArguments attributeArguments)
    {
        FieldName = fieldName;
        FieldType = fieldType;
        Comments = comments;
        AttributeArguments = attributeArguments;
    }

    public string FieldName { get; set; }
    public TypeSyntax FieldType { get; set; }
    public string[] Comments { get; set; } = Array.Empty<string>();
    public HashSet<string> FieldTypeNamespaces { get; set; } = new HashSet<string>();
    public MakePropertyArguments AttributeArguments { get; set; }

    public override string ToString()
    {
        return $"{FieldType} {FieldName} => {AttributeArguments.Name}";
    }
}

