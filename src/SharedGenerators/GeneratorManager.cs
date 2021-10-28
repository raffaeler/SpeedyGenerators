using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SpeedyGenerators
{
    internal partial class GeneratorManager
    {
        public SourceText GenerateINPCClass(string? @namespace, string className, ClassInfo classInfo)
        {
            ClassGenerator gen = new(@namespace, className);
            var modifiers = classInfo.ClassDeclaration.Modifiers;
            if (modifiers != default(SyntaxTokenList))
            {
                gen.Modifiers = modifiers;
            }

            gen.Usings.Add("System");
            gen.Usings.Add("System.Collections.Generic");
            gen.Usings.Add("System.ComponentModel");
            gen.Usings.Add("System.Runtime.CompilerServices");

            gen.EnableNullable = true;

            gen.Interfaces.Add("INotifyPropertyChanged");

            if (classInfo.GenerateEvent)
            {
                gen.Members.Add(gen.CreateEventField(
                    new[] { "Event triggered when a property changes its value" },
                    "PropertyChangedEventHandler?", "PropertyChanged", false, false));

                gen.Members.Add(gen.CreateOnPropertyChanged(ClassInfo.OnPropertyChangedMethodName));
            }

            foreach (var field in classInfo.Fields)
            {
                if (field == null || field.AttributeArguments == null ||
                    field.FieldType == null || field.FieldName == null) continue;

                if (field.FieldTypeNamespaces != null)
                {
                    if(classInfo.NamespaceName != null)
                        field.FieldTypeNamespaces.Remove(classInfo.NamespaceName);
                    
                    foreach(var ns in field.FieldTypeNamespaces)
                        gen.Usings.Add(ns);
                }

                var partialMethod = field.AttributeArguments.ExtraNotify
                    ? $"On{field.AttributeArguments.Name}Changed"
                    : null;

                gen.Members.Add(gen.CreatePropertyWithPropertyChanged(
                    field.Comments,
                    field.FieldType,
                    field.AttributeArguments.Name,
                    field.FieldName,
                    classInfo.TriggerMethodName,
                    partialMethod,
                    field.AttributeArguments.CompareValues,
                    false));

                if (partialMethod != null)
                {
                    gen.Members.Add(gen.CreatePartialMethod(partialMethod, gen.GetVoidTypeName(),
                        (field.FieldType, "oldValue"), (field.FieldType, "newValue")));
                }
            }


            //gen.Members.Add(gen.CreateField(
            //    new[] { "comment" }, "string", "_test", null, true, false));

            var source = gen.Generate();

            return source;
        }

    }
}
