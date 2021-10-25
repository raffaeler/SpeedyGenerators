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
        public SourceText GenerateINPCClass(string? @namespace, string className,
            IList<FieldInfo> fieldInfos)
        {
            ClassGenerator gen = new(@namespace, className);
            var modifiers = fieldInfos.FirstOrDefault()?.ClassModifiers;
            if (modifiers != default(SyntaxTokenList))
            {
#pragma warning disable CS8629 // Nullable value type may be null.
                gen.Modifiers = modifiers.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
            }

            gen.Usings.Add("System");
            gen.Usings.Add("System.Collections.Generic");
            gen.Usings.Add("System.ComponentModel");
            gen.Usings.Add("System.Runtime.CompilerServices");

            gen.EnableNullable = true;

            gen.Interfaces.Add("INotifyPropertyChanged");
            gen.Members.Add(gen.CreateEventField(
                new[] { "Event triggered when a property changes its value" },
                "PropertyChangedEventHandler?", "PropertyChanged", false, false));

            var onPropertyChangedMethodName = "OnPropertyChanged";
            gen.Members.Add(gen.CreateOnPropertyChanged(onPropertyChangedMethodName));

            foreach (var field in fieldInfos)
            {
                if (field == null || field.AttributeArguments == null ||
                    field.FieldType == null || field.FieldName == null) continue;

                if (field.FieldTypeNamespace != null
                    && field.FieldTypeNamespace != field.NamespaceName)
                {
                    gen.Usings.Add(field.FieldTypeNamespace);
                }

                var partialMethod = field.AttributeArguments.ExtraNotify
                    ? $"On{field.AttributeArguments.Name}Changed"
                    : null;

                gen.Members.Add(gen.CreatePropertyWithPropertyChanged(
                    field.Comments,
                    field.FieldType,
                    field.AttributeArguments.Name,
                    field.FieldName,
                    onPropertyChangedMethodName,
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
