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
            if (context.SyntaxReceiver == null) return;
            var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver;

            var filenamesLookup = CreateNames(syntaxReceiver.FieldInfos);

            foreach (var kvp in syntaxReceiver.FieldInfos)
            {
                var fieldInfos = kvp.Value;
                var syntaxTree = fieldInfos[0].SyntaxTree;
                if(syntaxTree==null) return;
            
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.FieldType == null ||
                        fieldInfo.FieldType is PredefinedTypeSyntax) continue;
                
                    var typeInfo = semanticModel.GetTypeInfo(fieldInfo.FieldType);
                    fieldInfo.FieldTypeNamespace = typeInfo.Type?.ContainingNamespace.ToString();
                }

                // assume the syntax receiver provide "things" coming from the same class
                var className = fieldInfos.FirstOrDefault()?.ClassName;
                var namespaceName = fieldInfos.FirstOrDefault()?.NamespaceName;

                if (className == null) return;
                if (!fieldInfos.Any(f => f.AttributeArguments != null)) return;

                var mgr = new GeneratorManager();
                var result = mgr.GenerateINPCClass(namespaceName, className, fieldInfos);

                var hintName = filenamesLookup[kvp.Key];
                context.AddSource(hintName, result);
            }
        }

        /// <summary>
        /// Generate a new dictionary:
        /// key => namespace.name (identical to the incoming dictionary)
        /// value => a new unique name for the filename
        /// The value can be just the class name or something more complex
        /// whenever multiple classes with the same name (but different namespace) exists
        /// </summary>
        private Dictionary<string, string> CreateNames(
            Dictionary<string, List<FieldInfo>> fieldInfos)
        {
            int i = 0;
            Dictionary<string, string> result = new();
            HashSet<string> values = new();
            foreach (var kvp in fieldInfos)
            {
                var firstItem = kvp.Value.FirstOrDefault();
                if (firstItem == null || firstItem.ClassName == null) continue;

                var uniqueName = GenerateUnique(values, firstItem.ClassName, ref i);
                result[kvp.Key] = uniqueName;
                values.Add(uniqueName);
            }

            return result;

            static string GenerateUnique(HashSet<string> values, string name, ref int i)
            {
                var tempName = name;
                while(values.Contains(tempName))
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
            internal Dictionary<string, List<FieldInfo>> FieldInfos { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
                {
                    var fieldInfo = new FieldInfo();
                    fieldInfo.SyntaxTree = syntaxNode.SyntaxTree;

                    var editedClass = syntaxNode.Ancestors()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault();

                    if(editedClass == null) return;
                    fieldInfo.ClassModifiers = editedClass.Modifiers;
                    fieldInfo.NamespaceName = editedClass.Ancestors()
                        .OfType<NamespaceDeclarationSyntax>()
                        .FirstOrDefault()
                        ?.Name
                        ?.ToString();
                    fieldInfo.NamespaceName ??= String.Empty;

                    fieldInfo.ClassName = editedClass.Identifier.ToString();
                    var fullName = $"{fieldInfo.NamespaceName}.{fieldInfo.ClassName}";
                    if (!FieldInfos.TryGetValue(fullName, out List<FieldInfo> fieldInfos))
                    {
                        fieldInfos = new List<FieldInfo>();
                        FieldInfos[fullName] = fieldInfos;
                    }

                    fieldInfos.Add(fieldInfo);

                    fieldInfo.FieldName = fieldDeclaration?.Declaration
                        ?.Variables.FirstOrDefault()
                        ?.Identifier.ToString();
                    if (fieldInfo.FieldName == null) return;

                    fieldInfo.FieldType = fieldDeclaration?.Declaration?.Type;

                    if (fieldDeclaration == null) return;

                    var attribute = fieldDeclaration.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Where(a => a.Name.ToString() == "MakeProperty")
                        .FirstOrDefault();

                    if(attribute != null)
                    {
                        fieldInfo.AttributeArguments = Extractor.ExtractAttributeArguments(attribute);
                        fieldInfo.Comments = Extractor.ExtractComments(fieldDeclaration);
                    }

                    
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

    }

}

