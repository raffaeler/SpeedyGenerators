using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace SpeedyGenerators;

public static class Diagnostics
{
    public static Dictionary<string, DiagnosticDescriptor> Messages = new Dictionary<string, DiagnosticDescriptor>()
    {
        { "SM01", new DiagnosticDescriptor(
                id: "SM01",
                title: "String not valid",
                messageFormat: "{0} string contain invalid character(s): '{1}'",
                category: "Parser",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true) },

        { "SM02", new DiagnosticDescriptor(
                    id: "SM02",
                    title: "Duplicates are not allowed",
                    messageFormat: "Duplicate {0}s are not allowed: skipping '{1}'",
                    category: "Parser",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true) },

        { "SM03", new DiagnosticDescriptor(
                    id: "SM03",
                    title: "Undefined Concept",
                    messageFormat: "Concept '{0}' must be also defined and not just used as a context",
                    category: "Parser",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true) },

        { "SM04", new DiagnosticDescriptor(
                    id: "SM04",
                    title: "Invalid Term[ConceptSpecifier]",
                    messageFormat: "The item'{0}' is not a valid 'Term[Specifier]'",
                    category: "Parser",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true) },

    };
}
