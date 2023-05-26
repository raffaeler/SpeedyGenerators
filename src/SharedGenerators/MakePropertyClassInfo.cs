using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class MakePropertyClassInfo
    {
        public static readonly string PropertyChangedEventName = "PropertyChanged";
        public static readonly string OnPropertyChangedMethodName = "OnPropertyChanged";
        public static readonly string DefaultGlobalPartialMethodName = "OnOnePropertyHasChanged";

        public MakePropertyClassInfo(ClassDeclarationSyntax classDeclaration, string namespaceName, string className)
        {
            ClassDeclaration = classDeclaration;
            NamespaceName = namespaceName;
            ClassName = className;
            FullName = $"{NamespaceName}.{ClassName}";
        }

        public ClassDeclarationSyntax ClassDeclaration { get; private set; }
        public string NamespaceName { get; private set; }
        public string ClassName { get; private set; }
        public string FullName { get; private set; }
        public List<string> FullNameBaseTypes { get; } = new();

        public bool GenerateEvent { get; set; } = true;
        public string TriggerMethodName { get; set; } = OnPropertyChangedMethodName;
        public string GlobalPartialMethodName { get; set; } = DefaultGlobalPartialMethodName;

        public List<FieldInfo> Fields { get; } = new();

        public override string ToString()
        {
            return $"{FullName} ({Fields.Count})";
        }
    }
}
