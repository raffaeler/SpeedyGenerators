using System;
using System.Collections.Generic;
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

            if (hasBaseType == null || !hasBaseType.Value) return true;

            return false;
        }
    }
}
