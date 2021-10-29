﻿using System;
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

        public static INamedTypeSymbol? SearchBaseTypes(this ClassDeclarationSyntax classDeclaration,
            SemanticModel model, Func<INamedTypeSymbol, bool> func)
        {
            var classTypeSymbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
            if (classTypeSymbol == null) return null;

            var baseTypeSymbol = classTypeSymbol.BaseType;
            while (baseTypeSymbol != null)
            {
                var found= func(baseTypeSymbol);
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
            return symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(s => s.Name.Contains("PropertyChanged") &&
                                s.Parameters.Length == 1 &&
                                (s.Parameters[0].Type.Name == "String" ||
                                s.Parameters[0].Type.Name == "string"))
                    .FirstOrDefault();
        }

        public static (bool generateEvent, string triggerName) GetPropertyChangedGenerationInfo(
            this ClassDeclarationSyntax classDeclaration, SemanticModel model)
        {
            var baseTypeWithEvent = classDeclaration.SearchBaseTypes(model,
                symbol => symbol.HasMemberCalled(ClassInfo.PropertyChangedEventName));

            if (baseTypeWithEvent == null) return (true, ClassInfo.OnPropertyChangedMethodName);

            var candidateMethod = baseTypeWithEvent.GetBestMatchForPropertyChangedMethod();
            if (candidateMethod == null) return (false, ClassInfo.OnPropertyChangedMethodName);

            return (false, candidateMethod.Name);
        }

    }
}
