using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SpeedyGenerators;

[Generator]
public partial class MakeConcreteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect type declarations with [MakeConcrete] attribute
        var typeInfos = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsTypeWithMakeConcreteAttribute(node),
            transform: static (ctx, ct) => TransformType(ctx, ct)
        ).Where(static info => info != null);

        // Combine with compilation for semantic analysis (resolving interface types)
        var combined = typeInfos.Collect().Combine(context.CompilationProvider);

        // Generate source
        context.RegisterSourceOutput(combined, static (spc, pair) => GenerateSource(spc, pair.Left, pair.Right));
    }

    private static bool IsTypeWithMakeConcreteAttribute(SyntaxNode node)
    {
        if (node is not BaseTypeDeclarationSyntax typeDecl) return false;

        var concreteTypeKind = typeDecl switch
        {
            ClassDeclarationSyntax => ConcreteTypeKind.Class,
            StructDeclarationSyntax => ConcreteTypeKind.Struct,
            RecordDeclarationSyntax record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                ? ConcreteTypeKind.RecordStruct : ConcreteTypeKind.RecordClass,
            _ => (ConcreteTypeKind?)null,
        };

        if (concreteTypeKind == null) return false;

        return typeDecl.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() is "MakeConcrete" or "MakeConcreteAttribute");
    }

    private static TypePipelineData? TransformType(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var typeDeclaration = (BaseTypeDeclarationSyntax)ctx.Node;

        var concreteTypeKind = typeDeclaration switch
        {
            ClassDeclarationSyntax => ConcreteTypeKind.Class,
            StructDeclarationSyntax => ConcreteTypeKind.Struct,
            RecordDeclarationSyntax record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                ? ConcreteTypeKind.RecordStruct : ConcreteTypeKind.RecordClass,
            _ => (ConcreteTypeKind?)null,
        };

        if (concreteTypeKind == null) return null;

        var attribute = typeDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Select(a => (attribute: a, attribName: a.Name.ToString()))
            .FirstOrDefault(a => a.attribName is "MakeConcrete" or "MakeConcreteAttribute");

        if (attribute.attribute == null) return null;

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

        namespaceName ??= string.Empty;
        var typeName = typeDeclaration.Identifier.ToString();
        var fullName = $"{namespaceName}.{typeName}";

        var attributeArguments = Extractor.ExtractMakeConcreteArguments(attribute.attribute);
        if (attributeArguments == null) return null;

        return new TypePipelineData(
            concreteTypeKind.Value,
            namespaceName,
            typeName,
            fullName,
            attributeArguments);
    }

    private static void GenerateSource(
        SourceProductionContext spc,
        ImmutableArray<TypePipelineData?> typeInfos,
        Compilation compilation)
    {
        try
        {
            var classInfos = new Dictionary<string, MakeConcreteTypeInfo>();

            foreach (var data in typeInfos)
            {
                if (!data.HasValue) continue;
                var d = data.Value;

                // Find the type declaration in the compilation
                var typeDeclaration = FindTypeDeclaration(compilation, d.NamespaceName, d.TypeName);
                if (typeDeclaration == null) continue;

                var classInfo = new MakeConcreteTypeInfo(d.ConcreteTypeKind,
                    typeDeclaration, d.NamespaceName, d.TypeName, d.AttributeArguments);

                // Retrieve base types
                var syntaxTree = typeDeclaration.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                classInfo.FullNameBaseTypes.AddRange(typeDeclaration.GetBaseTypes(semanticModel)
                    .Select(s => s.GetFullTypeName()));

                if (string.IsNullOrEmpty(classInfo.AttributeArguments.MockingFullTypeName))
                {
                    ReportDiagnosticsInterfaceTypeNotLoaded(spc, string.Empty);
                    return;
                }

                classInfos[d.FullName] = classInfo;
            }

            var filenamesLookup = CreateFilenames(classInfos);

            foreach (var kvp in classInfos)
            {
                var fullName = kvp.Key;
                var classInfo = kvp.Value;
                var syntaxTree = classInfo.TypeDeclaration.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                INamedTypeSymbol? ifaceSymbol = compilation.GetTypeByMetadataName(
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

                classInfo.MockingTypeName = ifaceSymbol!.Name;
                var propertySymbols = ifaceSymbol?.GetMembers().OfType<IPropertySymbol>();
                foreach (var propertySymbol in propertySymbols ?? Array.Empty<IPropertySymbol>())
                {
                    var propertyNameSymbol = propertySymbol.Name;
                    var propertyTypeSymbol = propertySymbol.Type.Name;

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
                            propertyType = SyntaxFactory.NullableType(propertyType);
                        }
                    }
                    else
                    {
                        if (classInfo.AttributeArguments.MakeValueTypesNullable)
                        {
                            propertyType = SyntaxFactory.NullableType(propertyType);
                        }
                    }

                    var propertyGenerationInfo = new PropertyGenerationInfo(propertyNameSymbol, propertyType);
                    classInfo.Properties.Add(propertyGenerationInfo);
                }

                var mgr = new GeneratorManager();
                var result = mgr.GenerateImplementationClass(classInfo);

                var hintName = filenamesLookup[fullName];
                spc.AddSource(hintName, result);
            }
        }
        catch (Exception err)
        {
            ReportDiagnostics(spc, err);
        }
    }

    private static BaseTypeDeclarationSyntax? FindTypeDeclaration(Compilation compilation,
        string namespaceName, string typeName)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var typeDecl in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (typeDecl.Identifier.ToString() != typeName) continue;

                var ns = typeDecl.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name?.ToString();

                ns ??= typeDecl.Ancestors()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name?.ToString();

                ns ??= string.Empty;

                if (ns == namespaceName) return typeDecl;
            }
        }

        return null;
    }

    private static string GetFullNamespaceFor(InterfaceDeclarationSyntax declarationSyntax)
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

    private static Dictionary<string, string> CreateFilenames(
        Dictionary<string, MakeConcreteTypeInfo> classInfos)
    {
        int i = 0;
        Dictionary<string, string> result = new();
        HashSet<string> values = new();
        foreach (var kvp in classInfos)
        {
            var className = kvp.Value.TypeName;
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

    private static void ReportDiagnostics(SourceProductionContext context, Exception exception)
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
            id: "MCG01",
            title: "General error",
            messageFormat: "An exception was generated: '{0}'",
            category: "MakeConcreteGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true), Location.None, exception.Message));
    }

    private static void ReportDiagnostics(SourceProductionContext context, string detail)
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
            id: "MCG02",
            title: "General syntax error",
            messageFormat: "A syntax error in the code is preventing the generation '{0}'",
            category: "MakeConcreteGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true), Location.None, detail));
    }

    private static void ReportDiagnosticsInterfaceTypeNotLoaded(SourceProductionContext context, string detail)
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
            id: "MCG03",
            title: "Invalid interface type",
            messageFormat: "The specified interface type cannot be null or empty '{0}'",
            category: "MakeConcreteGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true), Location.None, detail));
    }

    /// <summary>
    /// Pipeline data struct for type-level data extracted from syntax.
    /// </summary>
    private readonly struct TypePipelineData
    {
        public readonly ConcreteTypeKind ConcreteTypeKind;
        public readonly string NamespaceName;
        public readonly string TypeName;
        public readonly string FullName;
        public readonly MakeConcreteArguments AttributeArguments;

        public TypePipelineData(ConcreteTypeKind concreteTypeKind,
            string namespaceName,
            string typeName,
            string fullName,
            MakeConcreteArguments attributeArguments)
        {
            ConcreteTypeKind = concreteTypeKind;
            NamespaceName = namespaceName;
            TypeName = typeName;
            FullName = fullName;
            AttributeArguments = attributeArguments;
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

