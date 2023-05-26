using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators;

internal static class Utilities
{
    public const string NewLine = "\n";

    public static string toCamel(this string text)
    {
        if (string.IsNullOrEmpty(text)) { return text; }
        return $"{Char.ToLower(text[0])}{text.Substring(1)}";
    }

    public static List<FileInfo> GetFilesByPrefix(IEnumerable<AdditionalText> additionalFiles,
        string prefix)
    {
        var files = new List<FileInfo>();
        foreach (var additional in additionalFiles)
        {
            var fi = new FileInfo(additional.Path);
            var name = fi.Name;
            if (!name.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            files.Add(fi);
        }

        return files;
    }

    /// <summary>
    /// Walk a TypeSyntax down into the generic types and grabs all the unique
    /// namespaces.
    /// This function is used to know which using declaration add at the beginning
    /// of a source file
    /// </summary>
    public static void FillNamespaceChain(TypeSyntax? typeSyntax, SemanticModel model, HashSet<string> hashSet)
    {
        DescendTypeArguments(typeSyntax, t =>
        {
            var typeInfo = model.GetTypeInfo(t);
            var namespaceName = typeInfo.Type?.ContainingNamespace.ToString();
            if (namespaceName == null) return;
            hashSet.Add(namespaceName);
        });
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.typesyntax
    /// TypeSyntax derived classes
    /// *****************************
    /// - ArrayTypeSyntax              int[] x;
    /// - FunctionPointerTypeSyntax    delegate*<int, void> f
    /// - NameSyntax                   SomeType t; (AliasQualifiedNameSyntax, QualifiedNameSyntax, SimpleNameSyntax)
    /// - NullableTypeSyntax           SomeStruct? x;
    /// - OmittedTypeArgumentSyntax
    /// - PointerTypeSyntax            A* a;
    /// - PredefinedTypeSyntax         int c;
    /// - RefTypeSyntax                ref X x
    /// - TupleTypeSyntax              (SomeType x, SomeOtherType y) f;
    /// </summary>
    private static void DescendTypeArguments(TypeSyntax? typeSyntax, Action<TypeSyntax> invoker)
    {
        if (typeSyntax == null || typeSyntax is PredefinedTypeSyntax) return;

        if (typeSyntax is NameSyntax nameSyntax)
        {
            invoker(nameSyntax);
        }

        if (typeSyntax is GenericNameSyntax genericNameSyntax)
        {
            invoker(genericNameSyntax);
            foreach (var inner in genericNameSyntax.TypeArgumentList.Arguments)
            {
                DescendTypeArguments(inner, invoker);
            }

            return;
        }

        if (typeSyntax is ArrayTypeSyntax arrayTypeSyntax)
        {
            DescendTypeArguments(arrayTypeSyntax.ElementType, invoker);
            return;
        }

        if (typeSyntax is TupleTypeSyntax tupleTypeSyntax)
        {
            foreach (var element in tupleTypeSyntax.Elements)
            {
                DescendTypeArguments(element.Type, invoker);
            }

            return;
        }

        if (typeSyntax is NullableTypeSyntax nullableTypeSyntax)
        {
            DescendTypeArguments(nullableTypeSyntax.ElementType, invoker);
            return;
        }

        if (typeSyntax is PointerTypeSyntax pointerTypeSyntax)
        {
            DescendTypeArguments(pointerTypeSyntax.ElementType, invoker);

            return;
        }

        if (typeSyntax is FunctionPointerTypeSyntax functionPointerTypeSyntax)
        {
            foreach (var element in functionPointerTypeSyntax.ParameterList.Parameters)
            {
                DescendTypeArguments(element.Type, invoker);
            }

            return;
        }
    }


}



