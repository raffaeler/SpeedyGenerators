using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    public static class Extensions
    {
        public static bool HasBaseType(this ClassDeclarationSyntax classDeclaration)
        {
            var hasBaseType = classDeclaration.BaseList?.Types
                .FirstOrDefault()
                .IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleBaseType);

            if (hasBaseType == true) return true;

            return false;
        }

        public static string GetFullTypeName(this INamedTypeSymbol symbol)
            => $"{symbol.ContainingNamespace.Name}.{symbol.Name}";

        public static IList<INamedTypeSymbol> GetBaseTypes(this ClassDeclarationSyntax classDeclaration,
            SemanticModel model)
        {
            List<INamedTypeSymbol> result = new();
            var classTypeSymbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
            if (classTypeSymbol == null) return result;

            var baseTypeSymbol = classTypeSymbol.BaseType;
            while (baseTypeSymbol != null)
            {
                result.Add(baseTypeSymbol);
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }

            return result;
        }

        public static INamedTypeSymbol? SearchBaseTypes(this ClassDeclarationSyntax classDeclaration,
            SemanticModel model, Func<INamedTypeSymbol, bool> func)
        {
            var classTypeSymbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
            if (classTypeSymbol == null) return null;

            var baseTypeSymbol = classTypeSymbol.BaseType;
            while (baseTypeSymbol != null)
            {
                var found = func(baseTypeSymbol);
                if (found) return baseTypeSymbol;
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }

            return null;
        }

        public static bool HasMemberCalled(this INamedTypeSymbol symbol, string name)
        {
            if (symbol == null) return false;
            return symbol.GetMembers(name).Length > 0;
        }

        public static IMethodSymbol? GetBestMatchForPropertyChangedMethod(this INamedTypeSymbol symbol)
        {
            var candidates = symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(s => s.Name.Contains("PropertyChanged") &&
                                s.Parameters.Length == 1 &&
                                (s.Parameters[0].Type.Name == "String" ||
                                s.Parameters[0].Type.Name == "string"))
                    .ToList();
            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];
            var onPropertyChanged = candidates.FirstOrDefault(c => c.Name.StartsWith(MakePropertyClassInfo.OnPropertyChangedMethodName));
            if (onPropertyChanged != null) return onPropertyChanged;
            return candidates.First();
        }

        public static (bool generateEvent, string triggerName) GetPropertyChangedGenerationInfo(
            this ClassDeclarationSyntax classDeclaration, SemanticModel model)
        {
            var baseTypeWithEvent = classDeclaration.SearchBaseTypes(model,
                symbol => symbol.HasMemberCalled(MakePropertyClassInfo.PropertyChangedEventName));

            if (baseTypeWithEvent == null) return (true, MakePropertyClassInfo.OnPropertyChangedMethodName);

            var candidateMethod = baseTypeWithEvent.GetBestMatchForPropertyChangedMethod();
            if (candidateMethod == null) return (false, MakePropertyClassInfo.OnPropertyChangedMethodName);

            return (false, candidateMethod.Name);
        }

        public static bool CanGenerateGlobalPartialMethod(this ClassDeclarationSyntax classDeclaration, SemanticModel model,
            string globalPartialMethodName)
        {
            var baseClassDeclaringGlobalPartialMethod = classDeclaration.SearchBaseTypes(model, symbol =>
            {
                var members = symbol.GetMembers(globalPartialMethodName);
                foreach (var member in members.OfType<IMethodSymbol>())
                {
                    if (member.ReturnType.Name != "void") continue;
                    if (member.Parameters.Length != 1) continue;
                    var parameter = member.Parameters[0];
                    if (parameter.Type.Name != "string" && parameter.Type.Name != "String") continue;

                    return true;
                }

                return false;
            });

            return baseClassDeclaringGlobalPartialMethod == null;
        }

    }
}
