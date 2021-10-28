using System;
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
    public partial class PropertyChangedGenerator : ISourceGenerator
    {

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.SyntaxReceiver == null) return;
                var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver;
                var filenamesLookup = CreateFilenames(syntaxReceiver.ClassInfos);

                foreach (var kvp in syntaxReceiver.ClassInfos)
                {
                    var classInfo = kvp.Value;

                    var syntaxTree = classInfo.ClassDeclaration.SyntaxTree;
                    var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

                    var (generateEvent, triggerMethodName) = LookupBaseType(semanticModel, classInfo.ClassDeclaration);
                    classInfo.GenerateEvent = generateEvent;
                    classInfo.TriggerMethodName = triggerMethodName;

                    foreach (var fieldInfo in classInfo.Fields)
                    {
                        if (fieldInfo.FieldType == null || fieldInfo.FieldType is PredefinedTypeSyntax) continue;

                        Utilities.FillNamespaceChain(fieldInfo.FieldType, semanticModel, fieldInfo.FieldTypeNamespaces);
                    }

                    var mgr = new GeneratorManager();
                    var result = mgr.GenerateINPCClass(classInfo.NamespaceName, classInfo.ClassName, classInfo);

                    var hintName = filenamesLookup[kvp.Key];
                    context.AddSource(hintName, result);
                }
            }
            catch (Exception err)
            {
                ReportDiagnostics(context, err);
            }
        }

        private (bool generateEvent, string triggerName) LookupBaseType(SemanticModel model, ClassDeclarationSyntax classDeclaration)
        {
            var hasBaseType = classDeclaration.BaseList?.Types
                .FirstOrDefault()
                .IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleBaseType);
            if (hasBaseType == null || !hasBaseType.Value) return (true, ClassInfo.OnPropertyChangedMethodName);

            var classTypeSymbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
            if (classTypeSymbol == null) return (true, ClassInfo.OnPropertyChangedMethodName);

            var baseTypeSymbol = classTypeSymbol.BaseType;
            if (baseTypeSymbol == null) return (true, ClassInfo.OnPropertyChangedMethodName);

            var generateEvent = baseTypeSymbol.GetMembers(ClassInfo.PropertyChangedEventName).Length == 0;

            if (baseTypeSymbol.GetMembers(ClassInfo.OnPropertyChangedMethodName).Length > 0)
            {
                return (generateEvent, ClassInfo.OnPropertyChangedMethodName);
            }

            var existent = baseTypeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(s => s.Name.Contains("PropertyChanged") &&
                            s.Parameters.Length == 1 &&
                            s.Parameters[0].Type.Name == "String" || s.Parameters[0].Type.Name == "string")
                .FirstOrDefault();

            var guessMethod = existent != null ? existent.Name : ClassInfo.OnPropertyChangedMethodName;
            return (generateEvent, guessMethod);
        }

        /// <summary>
        /// Generate a new dictionary:
        /// key => namespace.name (identical to the incoming dictionary)
        /// value => a new unique name for the filename
        /// The value can be just the class name or something more complex
        /// whenever multiple classes with the same name (but different namespace) exists
        /// </summary>
        private Dictionary<string, string> CreateFilenames(
            Dictionary<string, ClassInfo> classInfos)
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

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            internal Dictionary<string, ClassInfo> ClassInfos { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
                {
                    if (fieldDeclaration == null) return;

                    var attribute = fieldDeclaration.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Select(a => (attribute: a, attribName: a.Name.ToString()))
                        .Where(a => a.attribName == "MakeProperty" ||
                                    a.attribName == "MakePropertyAttribute")
                        .FirstOrDefault();

                    if (attribute.attribute == null) return;

                    var editedClass = syntaxNode.Ancestors()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault();

                    if (editedClass == null) return;

                    var namespaceName = editedClass.Ancestors()
                        .OfType<NamespaceDeclarationSyntax>()
                        .FirstOrDefault()
                        ?.Name
                        ?.ToString();
                    namespaceName ??= String.Empty;
                    var className = editedClass.Identifier.ToString();
                    var fullName = $"{namespaceName}.{className}";

                    var fieldName = fieldDeclaration.Declaration
                        ?.Variables.FirstOrDefault()
                        ?.Identifier.ToString();
                    if (fieldName == null) return;

                    var fieldType = fieldDeclaration.Declaration?.Type;
                    if (fieldType == null) return;

                    var attributeArguments = Extractor.ExtractAttributeArguments(attribute.attribute);
                    if (attributeArguments == null) return;

                    var comments = Extractor.ExtractComments(fieldDeclaration);

                    if (!ClassInfos.TryGetValue(fullName, out ClassInfo classInfo))
                    {
                        classInfo = new ClassInfo(editedClass, namespaceName, className);
                        ClassInfos[fullName] = classInfo;
                    }

                    var fieldInfo = new FieldInfo(fieldName, fieldType, comments, attributeArguments);
                    classInfo.Fields.Add(fieldInfo);
                }
            }
        }

        private void ReportDiagnostics(GeneratorExecutionContext context, Exception exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                id: "PCG01",
                title: "General error",
                messageFormat: "An exception was generated: '{0}'",
                category: "PropertyChangedGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true), Location.None, exception.Message));
        }

        private void ReportDiagnostics(GeneratorExecutionContext context, string detail)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                id: "PCG02",
                title: "General syntax error",
                messageFormat: "A syntax error in the code is preventing the generation '{0}'",
                category: "PropertyChangedGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true), Location.None, detail));
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

    }

}

