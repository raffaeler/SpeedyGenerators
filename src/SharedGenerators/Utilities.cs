using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpeedyGenerators
{
    internal class Utilities
    {
        public List<FileInfo> GetFilesByPrefix(IEnumerable<AdditionalText> additionalFiles,
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
        public static HashSet<string> GetNamespaceChain(TypeSyntax? typeSyntax, SemanticModel model)
        {
            HashSet<string> ns = new();
            DescendTypeArguments(typeSyntax, t =>
            {
                var typeInfo = model.GetTypeInfo(t);
                var namespaceName = typeInfo.Type?.ContainingNamespace.ToString();
                if (namespaceName == null) return;
                ns.Add(namespaceName);
            });

            return ns;
        }
        private static void DescendTypeArguments(TypeSyntax? typeSyntax, Action<TypeSyntax> invoker)
        {
            if (typeSyntax == null) return;
            invoker(typeSyntax);
            if (typeSyntax is GenericNameSyntax genericNameSyntax)
            {
                foreach (var inner in genericNameSyntax.TypeArgumentList.Arguments)
                {
                    DescendTypeArguments(inner, invoker);
                }
            }
        }


    }
}
