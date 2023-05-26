﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SpeedyGenerators
{
    [Generator]
    public partial class MakeConcreteGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.SyntaxReceiver == null) return;
                var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver;
                var filenamesLookup = CreateFilenames(syntaxReceiver.ClassInfos);

                // First step: retrieve all the full type names for the concrete base types
                // of the type that needs to be augmented with the INPC code
                foreach (var classInfo in syntaxReceiver.ClassInfos.Values)
                {
                    var syntaxTree = classInfo.ClassDeclaration.SyntaxTree;
                    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                    classInfo.FullNameBaseTypes.AddRange(classInfo.ClassDeclaration.GetBaseTypes(semanticModel)
                        .Select(s => s.GetFullTypeName()));

                    if(string.IsNullOrEmpty(classInfo.AttributeArguments.InterfaceFullTypeName))
                    {
                        ReportDiagnosticsInterfaceTypeNotLoaded(context, string.Empty);
                        return;
                    }
                }

                foreach (var kvp in syntaxReceiver.ClassInfos)
                {
                    var classInfo = kvp.Value;

                    var syntaxTree = classInfo.ClassDeclaration.SyntaxTree;
                    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

                    ////var (generateEvent, triggerMethodName) =
                    ////    classInfo.ClassDeclaration.GetPropertyChangedGenerationInfo(semanticModel);

                    //var mgr = new GeneratorManager();
                    //var result = mgr.GenerateINPCClass(classInfo.NamespaceName, classInfo.ClassName, classInfo);

                    //var hintName = filenamesLookup[kvp.Key];
                    //context.AddSource(hintName, result);
                }
            }
            catch (Exception err)
            {
                ReportDiagnostics(context, err);
            }
        }

        /// <summary>
        /// Generate a new dictionary:
        /// key => namespace.name (identical to the incoming dictionary)
        /// value => a new unique name for the filename
        /// The value can be just the class name or something more complex
        /// whenever multiple classes with the same name (but different namespace) exists
        /// </summary>
        private Dictionary<string, string> CreateFilenames(
            Dictionary<string, MakeConcreteClassInfo> classInfos)
        {
            int i = 0;
            Dictionary<string, string> result = new();
            HashSet<string> values = new();
            foreach (var kvp in classInfos)
            {
                var className = kvp.Value.ClassName;

                var uniqueName = GenerateUnique(values, className, ref i);
                result[kvp.Key] = uniqueName;
                values.Add(uniqueName);
            }

            return result;

            static string GenerateUnique(HashSet<string> values, string name, ref int i)
            {
                var tempName = name;
                while (values.Contains(tempName))
                {
                    tempName = $"{tempName}{++i}";
                }

                return tempName;
            }
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            internal Dictionary<string, MakeConcreteClassInfo> ClassInfos { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclaration)
                {
                    if (classDeclaration == null) return;

                    var attribute = classDeclaration.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Select(a => (attribute: a, attribName: a.Name.ToString()))
                        .Where(a => a.attribName == "MakeConcrete" ||
                                    a.attribName == "MakeConcreteAttribute")
                        .FirstOrDefault();

                    if (attribute.attribute == null) return;

                    var namespaceName = classDeclaration.Ancestors()
                        .OfType<NamespaceDeclarationSyntax>()
                        .FirstOrDefault()
                        ?.Name
                        ?.ToString();

                    namespaceName ??= classDeclaration.Ancestors()
                        .OfType<FileScopedNamespaceDeclarationSyntax>()
                        .FirstOrDefault()
                        ?.Name
                        ?.ToString();

                    namespaceName ??= String.Empty;
                    var className = classDeclaration.Identifier.ToString();
                    var fullName = $"{namespaceName}.{className}";

                    var attributeArguments = Extractor.ExtractMakeConcreteArguments(attribute.attribute);
                    if (attributeArguments == null) return;

                    if (!ClassInfos.TryGetValue(fullName, out MakeConcreteClassInfo classInfo))
                    {
                        classInfo = new MakeConcreteClassInfo(classDeclaration,
                            namespaceName, className, attributeArguments);
                        ClassInfos[fullName] = classInfo;
                    }
                }
            }
        }

        private void ReportDiagnostics(GeneratorExecutionContext context, Exception exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                id: "MCG01",
                title: "General error",
                messageFormat: "An exception was generated: '{0}'",
                category: "MakeConcreteGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true), Location.None, exception.Message));
        }

        private void ReportDiagnostics(GeneratorExecutionContext context, string detail)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                id: "MCG02",
                title: "General syntax error",
                messageFormat: "A syntax error in the code is preventing the generation '{0}'",
                category: "MakeConcreteGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true), Location.None, detail));
        }

        private void ReportDiagnosticsInterfaceTypeNotLoaded(GeneratorExecutionContext context, string detail)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                id: "MCG03",
                title: "Invalid interface type",
                messageFormat: "The specified interface type cannot be null or empty '{0}'",
                category: "MakeConcreteGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true), Location.None, detail));
        }
    }
}

//private string _field;
//public string Field
//{
//    get => _field;
//    set
//    {
//        if (_field == value) return;
//        var old = _field;
//        _field = value;
//        OnPropertyChanged();
//        OnFieldChanged(old, _field);
//    }
//}

//partial void OnFieldChanged(string oldValue, string newValue);

//public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

//protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
//{
//    this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
//}

