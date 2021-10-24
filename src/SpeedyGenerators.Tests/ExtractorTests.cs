using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpeedyGenerators;

namespace SpeedyGenerators.Tests
{
    /// <summary>
    /// <para/>
    /// </summary>
    [TestClass]
    public class ExtractorTests
    {
        [TestMethod]
        public void ExtractCommentsTest()
        {
            SourceText sourceText = SourceText.From($@"
public partial class Test
{{
    /// <summary>
    /// Comment Line 1<para/>
    /// Comment Line 2
    /// </summary>
    private int _field;

    private void Method()
    {{
        // generated code
    }}
}}", Encoding.UTF8);

            var tree = CSharpSyntaxTree.ParseText(sourceText);

            var field = tree.GetRoot()
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Single();

            var commentLines = Extractor.ExtractComments(field);
            Assert.IsTrue(commentLines.Length == 3);
            Assert.AreEqual("Comment Line 1", commentLines[0]);
            Assert.AreEqual("<para/>", commentLines[1]);
            Assert.AreEqual("Comment Line 2", commentLines[2]);
        }

        [TestMethod]
        public void ExtractAttributeArgumentsTest()
        {
            SourceText sourceText = SourceText.From($@"
public partial class Test
{{
    [Test(""X"")]
    private int _x;

    [Test(""Y"", false)]
    private int _y;

    [Test(""Z"", true)]
    private int _z;

    [Test(""T"", true, true)]
    private int _t;
}}", Encoding.UTF8);

            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var fields = tree.GetRoot()
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .ToList();

            var cases = fields.SelectMany(GetAttributes).ToList();
            var arguments0 = Extractor.ExtractAttributeArguments(cases[0]);
            Assert.IsNotNull(arguments0);
            Assert.IsTrue(arguments0.Name == "X");
            Assert.IsTrue(arguments0.ExtraNotify == false);
            Assert.IsTrue(arguments0.CompareValues == false);

            var arguments1 = Extractor.ExtractAttributeArguments(cases[1]);
            Assert.IsNotNull(arguments1);
            Assert.IsTrue(arguments1.Name == "Y");
            Assert.IsTrue(arguments1.ExtraNotify == false);
            Assert.IsTrue(arguments1.CompareValues == false);

            var arguments2 = Extractor.ExtractAttributeArguments(cases[2]);
            Assert.IsNotNull(arguments2);
            Assert.IsTrue(arguments2.Name == "Z");
            Assert.IsTrue(arguments2.ExtraNotify == true);
            Assert.IsTrue(arguments2.CompareValues == false);

            var arguments3 = Extractor.ExtractAttributeArguments(cases[3]);
            Assert.IsNotNull(arguments3);
            Assert.IsTrue(arguments3.Name == "T");
            Assert.IsTrue(arguments3.ExtraNotify == true);
            Assert.IsTrue(arguments3.CompareValues == true);
        }

        private List<AttributeSyntax> GetAttributes(FieldDeclarationSyntax fieldDeclaration)
        {
            return fieldDeclaration.AttributeLists
                .SelectMany(a => a.Attributes)
                .ToList();
        }

    }
}


public partial class Test
{
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169
    [Test("X")]
    private readonly int _x;

    [Test("Y", false)]
    private readonly int _y;

    [Test("Z", true)]
    private readonly int _z;

    [Test("T", true, true)]
    private readonly int _t;
#pragma warning restore CS0169
#pragma warning restore IDE0051 // Remove unused private members
}

namespace SpeedyGenerators
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class TestAttribute : Attribute
    {
        public TestAttribute(string name, bool extraNotify = false, bool compareValues = false)
        {
            this.Name = name;
            this.ExtraNotify = extraNotify;
            this.CompareValues = compareValues;
        }

        public string Name { get; private set; }
        public bool ExtraNotify { get; private set; }
        public bool CompareValues { get; private set; }
    }
}
