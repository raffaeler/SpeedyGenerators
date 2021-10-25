using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class FieldInfo
    {
        public SyntaxTree? SyntaxTree { get; set; }
        public string? NamespaceName { get; set; }
        public string? ClassName { get; set; }
        public SyntaxTokenList ClassModifiers { get; set; }
        public string? FieldName { get; set; }
        public TypeSyntax? FieldType { get; set; }
        public string? FieldTypeNamespace { get; set; }
        public string[]? Comments { get; set; }
        public MakePropertyArguments? AttributeArguments { get; set; }
    }
}

