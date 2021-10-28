using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class ClassInfo
    {
        public static readonly string PropertyChangedEventName = "PropertyChanged";
        public static readonly string OnPropertyChangedMethodName = "OnPropertyChanged";

        public ClassInfo(ClassDeclarationSyntax classDeclaration, string namespaceName, string className)
        {
            ClassDeclaration = classDeclaration;
            NamespaceName = namespaceName;
            ClassName = className;
        }

        public ClassDeclarationSyntax ClassDeclaration { get; set; }
        public string NamespaceName { get; set; }
        public string ClassName { get; set; }

        public bool GenerateEvent { get; set; } = true;
        public string TriggerMethodName { get; set; } = OnPropertyChangedMethodName;

        public List<FieldInfo> Fields { get; } = new List<FieldInfo>();

        public override string ToString()
        {
            return $"{NamespaceName}.{ClassName} ({Fields.Count})";
        }
    }
}
