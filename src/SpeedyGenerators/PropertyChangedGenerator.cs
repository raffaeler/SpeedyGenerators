using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SpeedyGenerators;

[Generator]
public partial class PropertyChangedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect field declarations with [MakeProperty] attribute and extract per-field data
        var fieldInfos = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsFieldWithMakePropertyAttribute(node),
            transform: static (ctx, ct) => TransformField(ctx, ct)
        ).Where(static info => info != null);

        // Combine with compilation for class-level semantic analysis
        var combined = fieldInfos.Collect().Combine(context.CompilationProvider);

        // Generate source
        context.RegisterSourceOutput(combined, static (spc, pair) => GenerateSource(spc, pair.Left, pair.Right));
    }

    private static bool IsFieldWithMakePropertyAttribute(SyntaxNode node)
    {
        if (node is not FieldDeclarationSyntax fieldDeclaration) return false;
        return fieldDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() is "MakeProperty" or "MakePropertyAttribute");
    }

    private static FieldPipelineData? TransformField(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)ctx.Node;

        var attribute = fieldDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Select(a => (attribute: a, attribName: a.Name.ToString()))
            .FirstOrDefault(a => a.attribName is "MakeProperty" or "MakePropertyAttribute");

        if (attribute.attribute == null) return null;

        var editedClass = fieldDeclaration.Ancestors()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();
        if (editedClass == null) return null;

        var namespaceName = editedClass.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault()
            ?.Name
            ?.ToString();

        namespaceName ??= editedClass.Ancestors()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault()
            ?.Name
            ?.ToString();

        namespaceName ??= string.Empty;
        var className = editedClass.Identifier.ToString();
        var fullName = $"{namespaceName}.{className}";

        var fieldName = fieldDeclaration.Declaration?.Variables.FirstOrDefault()?.Identifier.ToString();
        if (fieldName == null) return null;

        var fieldType = fieldDeclaration.Declaration?.Type;
        if (fieldType == null) return null;

        var attributeArguments = Extractor.ExtractMakePropertyArguments(attribute.attribute);
        if (attributeArguments == null) return null;

        var comments = Extractor.ExtractComments(fieldDeclaration);

        return new FieldPipelineData(
            namespaceName,
            className,
            fullName,
            fieldName,
            fieldType,
            comments,
            attributeArguments);
    }

    private static void GenerateSource(
        SourceProductionContext spc,
        ImmutableArray<FieldPipelineData?> fieldInfos,
        Compilation compilation)
    {
        try
        {
            // Group fields by class
            var classGroups = fieldInfos
                .Where(f => f.HasValue)
                .Select(f => f!.Value)
                .GroupBy(f => f.FullClassName);

            var classInfos = new Dictionary<string, MakePropertyClassInfo>();

            foreach (var group in classGroups)
            {
                var first = group.First();
                // We need the ClassDeclarationSyntax for semantic analysis - retrieve from compilation
                var classDeclaration = FindClassDeclaration(compilation, first.NamespaceName, first.ClassName);

                if (classDeclaration == null) continue;

                var classInfo = new MakePropertyClassInfo(classDeclaration, first.NamespaceName, first.ClassName);
                classInfos[first.FullClassName] = classInfo;

                foreach (var fieldData in group)
                {
                    var fieldInfo = new MakePropertyFieldInfo(
                        fieldData.FieldName,
                        fieldData.FieldType,
                        fieldData.Comments,
                        fieldData.AttributeArguments);
                    classInfo.Fields.Add(fieldInfo);
                }
            }

            // Retrieve base type names for all classes
            foreach (var classInfo in classInfos.Values)
            {
                var syntaxTree = classInfo.ClassDeclaration.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                classInfo.FullNameBaseTypes.AddRange(classInfo.ClassDeclaration.GetBaseTypes(semanticModel)
                    .Select(s => s.GetFullTypeName()));
            }

            var filenamesLookup = CreateFilenames(classInfos);

            foreach (var kvp in classInfos)
            {
                var fullName = kvp.Key;
                var classInfo = kvp.Value;
                var syntaxTree = classInfo.ClassDeclaration.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var (generateEvent, triggerMethodName) =
                    classInfo.ClassDeclaration.GetPropertyChangedGenerationInfo(semanticModel);
                classInfo.GenerateEvent = generateEvent;
                classInfo.TriggerMethodName = triggerMethodName;

                if (!classInfo.ClassDeclaration.CanGenerateGlobalPartialMethod(semanticModel, classInfo.GlobalPartialMethodName))
                {
                    classInfo.GlobalPartialMethodName = string.Empty;
                }
                else
                {
                    var allClassInfos = classInfos.Values;
                    foreach (var baseClassName in classInfo.FullNameBaseTypes)
                    {
                        if (allClassInfos.Any(c => c.FullName == baseClassName))
                        {
                            classInfo.GenerateEvent = false;
                            classInfo.GlobalPartialMethodName = string.Empty;
                            break;
                        }
                    }
                }

                foreach (var fieldInfo in classInfo.Fields)
                {
                    if (fieldInfo.FieldType == null || fieldInfo.FieldType is PredefinedTypeSyntax) continue;

                    Utilities.FillNamespaceChain(fieldInfo.FieldType, semanticModel, fieldInfo.FieldTypeNamespaces);
                }

                var mgr = new GeneratorManager();
                var result = mgr.GenerateINPCClass(classInfo.NamespaceName, classInfo.ClassName, classInfo);

                var hintName = filenamesLookup[fullName];
                spc.AddSource(hintName, result);
            }
        }
        catch (Exception err)
        {
            ReportDiagnostics(spc, err);
        }
    }

    /// <summary>
    /// Find a ClassDeclarationSyntax for the given namespace and class name
    /// from all syntax trees in the compilation.
    /// </summary>
    private static ClassDeclarationSyntax? FindClassDeclaration(Compilation compilation,
        string namespaceName, string className)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDecl.Identifier.ToString() != className) continue;

                var ns = classDecl.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name?.ToString();

                ns ??= classDecl.Ancestors()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name?.ToString();

                ns ??= string.Empty;

                if (ns == namespaceName) return classDecl;
            }
        }

        return null;
    }

    private static Dictionary<string, string> CreateFilenames(
        Dictionary<string, MakePropertyClassInfo> classInfos)
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

    private static void ReportDiagnostics(SourceProductionContext context, Exception exception)
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
            id: "PCG01",
            title: "General error",
            messageFormat: "An exception was generated: '{0}'",
            category: "PropertyChangedGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true), Location.None, exception.Message));
    }

    private static void ReportDiagnostics(SourceProductionContext context, string detail)
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
            id: "PCG02",
            title: "General syntax error",
            messageFormat: "A syntax error in the code is preventing the generation '{0}'",
            category: "PropertyChangedGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true), Location.None, detail));
    }

    /// <summary>
    /// Pipeline data struct - per-field data extracted from syntax,
    /// used as the intermediate representation between the syntax provider
    /// and the source generation phase.
    /// </summary>
    private readonly struct FieldPipelineData
    {
        public readonly string NamespaceName;
        public readonly string ClassName;
        public readonly string FullClassName;
        public readonly string FieldName;
        public readonly TypeSyntax FieldType;
        public readonly string[] Comments;
        public readonly MakePropertyArguments AttributeArguments;

        public FieldPipelineData(
            string namespaceName,
string className,
            string fullClassName,
            string fieldName,
            TypeSyntax fieldType,
            string[] comments,
            MakePropertyArguments attributeArguments)
        {
            NamespaceName = namespaceName;
            ClassName = className;
            FullClassName = fullClassName;
            FieldName = fieldName;
            FieldType = fieldType;
            Comments = comments;
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

