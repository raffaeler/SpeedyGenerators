﻿using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class MakeConcreteClassInfo
    {
        public static readonly string PropertyChangedEventName = "PropertyChanged";
        public static readonly string OnPropertyChangedMethodName = "OnPropertyChanged";
        public static readonly string DefaultGlobalPartialMethodName = "OnOnePropertyHasChanged";

        public MakeConcreteClassInfo(ClassDeclarationSyntax classDeclaration, string namespaceName, string className,
            MakeConcreteArguments arguments)
        {
            ClassDeclaration = classDeclaration;
            NamespaceName = namespaceName;
            ClassName = className;
            AttributeArguments = arguments;
            FullName = $"{NamespaceName}.{ClassName}";
        }

        public ClassDeclarationSyntax ClassDeclaration { get; private set; }
        public string NamespaceName { get; private set; }
        public string ClassName { get; private set; }
        public string MockingTypeName { get; set; } = string.Empty;
        public MakeConcreteArguments AttributeArguments { get; }
        public string FullName { get; private set; }
        public List<string> FullNameBaseTypes { get; } = new();
        public List<PropertyGenerationInfo> Properties = new();

        public override string ToString()
        {
            return $"{FullName} ";
        }
    }
}
