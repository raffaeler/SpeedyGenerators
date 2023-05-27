using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                    var syntaxTree = classInfo.TypeDeclaration.SyntaxTree;
                    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                    classInfo.FullNameBaseTypes.AddRange(classInfo.TypeDeclaration.GetBaseTypes(semanticModel)
                        .Select(s => s.GetFullTypeName()));

                    if (string.IsNullOrEmpty(classInfo.AttributeArguments.MockingFullTypeName))
                    {
                        ReportDiagnosticsInterfaceTypeNotLoaded(context, string.Empty);
                        return;
                    }
                }

                foreach (var kvp in syntaxReceiver.ClassInfos)
                {
                    var classInfo = kvp.Value;

                    var syntaxTree = classInfo.TypeDeclaration.SyntaxTree;
                    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

                    INamedTypeSymbol? ifaceSymbol = context.Compilation.GetTypeByMetadataName(
                        classInfo.AttributeArguments.MockingFullTypeName);
                    if (ifaceSymbol?.Name == null) continue;
                    var ifaceDeclarationSyntax = ifaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                        as InterfaceDeclarationSyntax;
                    if (ifaceDeclarationSyntax != null)
                    {
                        var nspace = GetFullNamespaceFor(ifaceDeclarationSyntax);
                        if (nspace != null && classInfo.AttributeArguments.ImplementInterface)
                        {
                            classInfo.Namespaces.Add(nspace.ToString());
                        }
                    }

                    classInfo.MockingTypeName = ifaceSymbol.Name;
                    var propertySymbols = ifaceSymbol?.GetMembers().OfType<IPropertySymbol>();
                    // to go back to the syntax use .DeclaringSyntaxReferences
                    foreach (var propertySymbol in propertySymbols ?? Array.Empty<IPropertySymbol>())
                    {
                        var propertyNameSymbol = propertySymbol.Name;         // propertySymbol.MetadataName
                        var propertyTypeSymbol = propertySymbol.Type.Name;    // propertySymbol.Type.MetadataName

                        var nameSyntax = SyntaxFactory.ParseName(propertyTypeSymbol);
                        var propertyDeclarationSyntax = propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                             as PropertyDeclarationSyntax;
                        if (propertyDeclarationSyntax?.Type == null) continue;

                        var propertyType = propertyDeclarationSyntax.Type;
                        Utilities.FillNamespaceChain(propertyType, semanticModel, classInfo.Namespaces);

                        if (propertySymbol.Type.IsReferenceType)
                        {
                            if (classInfo.AttributeArguments.MakeReferenceTypesNullable)
                            {
                                // makes the type nullable
                                propertyType = SyntaxFactory.NullableType(propertyType);
                            }
                        }
                        else
                        {
                            if (classInfo.AttributeArguments.MakeValueTypesNullable)
                            {
                                // it is a value type
                                propertyType = SyntaxFactory.NullableType(propertyType);
                            }
                        }

                        var propertyGenerationInfo = new PropertyGenerationInfo(propertyNameSymbol, propertyType);
                        classInfo.Properties.Add(propertyGenerationInfo);
                    }

                    //foreach (var property in classInfo.Properties)
                    //{
                    //    if (property.PropertyType == null) continue;

                    //    Utilities.FillNamespaceChain(property.PropertyType, semanticModel, classInfo.Namespaces);
                    //}

                    var mgr = new GeneratorManager();
                    var result = mgr.GenerateImplementationClass(classInfo);

                    var hintName = filenamesLookup[kvp.Key];
                    context.AddSource(hintName, result);
                }
            }
            catch (Exception err)
            {
                ReportDiagnostics(context, err);
            }
        }

        private string GetFullNamespaceFor(InterfaceDeclarationSyntax declarationSyntax)
        {
            SyntaxNode? node = declarationSyntax;
            List<string> names = new();
            while (node != null)
            {
                if (node is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                {
                    names.Insert(0, namespaceDeclarationSyntax.Name.ToString());
                }
                else if (node is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
                {
                    names.Insert(0, fileScopedNamespaceDeclarationSyntax.Name.ToString());
                }
                node = node.Parent;
            }

            return string.Join(".", names);
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
                BaseTypeDeclarationSyntax? typeDeclaration = syntaxNode as BaseTypeDeclarationSyntax;
                if (typeDeclaration == null) return;

                ConcreteTypeKind? concreteTypeKind = typeDeclaration switch
                {
                    ClassDeclarationSyntax => ConcreteTypeKind.Class,
                    StructDeclarationSyntax => ConcreteTypeKind.Struct,
                    RecordDeclarationSyntax record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                        ? ConcreteTypeKind.RecordStruct : ConcreteTypeKind.RecordClass,
                    _ => null,
                };
                if (concreteTypeKind == null) return;

                var attribute = typeDeclaration.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Select(a => (attribute: a, attribName: a.Name.ToString()))
                    .Where(a => a.attribName == "MakeConcrete" ||
                                a.attribName == "MakeConcreteAttribute")
                    .FirstOrDefault();

                if (attribute.attribute == null) return;

                var namespaceName = typeDeclaration.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name
                    ?.ToString();

                namespaceName ??= typeDeclaration.Ancestors()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name
                    ?.ToString();

                namespaceName ??= String.Empty;
                var className = typeDeclaration.Identifier.ToString();
                var fullName = $"{namespaceName}.{className}";

                var attributeArguments = Extractor.ExtractMakeConcreteArguments(attribute.attribute);
                if (attributeArguments == null) return;

                if (!ClassInfos.TryGetValue(fullName, out MakeConcreteClassInfo classInfo))
                {
                    classInfo = new MakeConcreteClassInfo(concreteTypeKind.Value, typeDeclaration,
                        namespaceName, className, attributeArguments);
                    ClassInfos[fullName] = classInfo;
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

