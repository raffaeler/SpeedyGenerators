using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    [Generator]
    public partial class PropertyChangedGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver == null) return;
            var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver;
            var fieldInfos = syntaxReceiver.FieldInfos;

            // assume the syntax receiver provide "things" coming from the same class
            var className = fieldInfos.FirstOrDefault()?.ClassName;
            var namespaceName = fieldInfos.FirstOrDefault()?.NamespaceName;

            if (className == null) return;
            if (!fieldInfos.Any(f => f.AttributeArguments != null)) return;

            var mgr = new GeneratorManager();
            var result = mgr.GenerateINPCClass(namespaceName, className, fieldInfos);

            if(result != null) context.AddSource(className, result);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            internal List<FieldInfo> FieldInfos { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                //if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                //    classDeclaration.Modifiers
                //        .Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
                //{
                //    EditedClass = classDeclaration;
                //}
                //else
                //{
                //    EditedClass = null;
                //}

                if (syntaxNode is FieldDeclarationSyntax fieldDeclaration)
                {
                    var fieldInfo = new FieldInfo();
                    FieldInfos.Add(fieldInfo);

                    var editedClass = syntaxNode.Ancestors()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault();

                    if(editedClass == null) return;
                    fieldInfo.NamespaceName = editedClass.Ancestors()
                        .OfType<NamespaceDeclarationSyntax>()
                        .FirstOrDefault()
                        ?.Name
                        ?.ToString();

                    fieldInfo.ClassName = editedClass.Identifier.ToString();

                    fieldInfo.FieldName = fieldDeclaration?.Declaration
                        ?.Variables.FirstOrDefault()
                        ?.Identifier.ToString();
                    if (fieldInfo.FieldName == null) return;

                    fieldInfo.FieldType = fieldDeclaration?.Declaration.Type;

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

