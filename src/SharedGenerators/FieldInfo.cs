using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class FieldInfo
    {
        public string? NamespaceName { get; set; }
        public string? ClassName { get; set; }
        public string? FieldName { get; set; }
        public TypeSyntax? FieldType { get; set; }
        public string[]? Comments { get; set; }
        public MakePropertyArguments? AttributeArguments { get; set; }
    }
}

