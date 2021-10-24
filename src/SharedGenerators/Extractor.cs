using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal static class Extractor
    {
        public static string[] ExtractComments(FieldDeclarationSyntax fieldDeclaration)
        {
            return ExtractComments(fieldDeclaration.GetLeadingTrivia());
        }

        public static string[] ExtractComments(SyntaxTriviaList syntaxTrivias)
        {
            List<string> lines = new();
            foreach (var trivia in syntaxTrivias)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    var structure = trivia.GetStructure()
                        as DocumentationCommentTriviaSyntax;
                    if (structure == null) continue;

                    foreach (var contentLine in structure.Content
                        .OfType<XmlElementSyntax>())
                    {
                        foreach (var xml in contentLine.Content)
                        {
                            lines.Add(xml.ToString()
                                .Trim(new[] { '\r', '\n', '/', ' ' }));
                        }
                    }
                }
            }

            return lines.ToArray();
        }

        public static MakePropertyArguments? ExtractAttributeArguments(
            AttributeSyntax attribute)
        {
            var args = attribute.ArgumentList?.Arguments
                .Select(a => a.ToString())
                .ToArray();

            MakePropertyArguments.TryParse(args, out MakePropertyArguments? arguments);

            return arguments;
        }
    }
}
