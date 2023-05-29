using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpeedyGenerators.Tests;

[TestClass]
public class SemanticTests
{
    [TestMethod]
    public void ReadFieldType()
    {
        SourceText sourceText = SourceText.From(@"
namespace SomeNamespace
{
    public partial class SomeType { }

}

namespace AnotherNamespace
{
    using SomeNamespace;
    public partial class Test
    {
        private SomeType _field;
    }
}", Encoding.UTF8);

        var tree = CSharpSyntaxTree.ParseText(sourceText);
        if (tree == null) { Assert.Fail("Tree is null"); return; }

        var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
        var model = compilation.GetSemanticModel(tree);

        var field = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Single();

        var declaration = field.Declaration;
        var type = declaration.Type;
        var typeInfo = model.GetTypeInfo(type);
        var namespaceName = typeInfo.Type?.ContainingNamespace.ToString();
        Assert.AreEqual("SomeNamespace", namespaceName);
    }

    [TestMethod]
    public void ExtractNamespaces()
    {
        SourceText sourceText = SourceText.From(@"
namespace SomeNamespace
{
    public partial class SomeType { }
}

namespace NamespaceGen
{
    public class Gen<T>
    {
        public T Instance { get; set; }
    }
}

namespace AnotherNamespace
{
    using SomeNamespace;
    using NamespaceGen;
    public partial class Test
    {
        private Gen<Gen<SomeType>> _field;
    }
}", Encoding.UTF8);

        var (fields, model) = GetFields(sourceText);

        if (model == null) { Assert.Fail(); return; }
        var declaration = fields.Single().Declaration;

        HashSet<string> ns = new();
        Utilities.FillNamespaceChain(declaration.Type, model, ns);

        if (ns == null) { Assert.Fail(); }
        else
        {
            Assert.IsTrue(ns == null
                ? false
                : ns.SequenceEqual(new[] { "NamespaceGen", "SomeNamespace" }));
        }
    }

    [TestMethod]
    public void ExtractNamespaces2()
    {
        SourceText sourceText = SourceText.From(@"
namespace NamespaceA1
{
    public partial class A1 { }
}

namespace NamespaceA1.NamespaceA2
{
    public partial class A2 { }
}

namespace NamespaceB
{
    public record Gen<T>(T Instance);
}

namespace AnotherNamespace
{
    using NamespaceA1;
    using NamespaceA1.NamespaceA2;

    using NamespaceB;
    public partial class Test
    {
        private A1[] _field0;
        private Gen<Gen<A1>[]> _field1;
        private (A1, A2) _field2;
        private (A1, A2?) _field3;
        private (A1, A2)? _field4;
        private Gen<Gen<(A1, A2)>> _field5;
        private int _field6;
        private int[] _field7;
        private delegate*<A1?, A2[]> _field8;
        private A1* _field9;
    }
}", Encoding.UTF8);


        var (fields, model) = GetFields(sourceText);
        Assert.IsTrue(fields.Count == 10);
        if (model == null) { Assert.Fail(); return; }

        HashSet<string> ns0 = new();
        HashSet<string> ns1 = new();
        HashSet<string> ns2 = new();
        HashSet<string> ns3 = new();
        HashSet<string> ns4 = new();
        HashSet<string> ns5 = new();
        HashSet<string> ns6 = new();
        HashSet<string> ns7 = new();
        HashSet<string> ns8 = new();
        HashSet<string> ns9 = new();

        Utilities.FillNamespaceChain(fields[0].Declaration.Type, model, ns0);
        Utilities.FillNamespaceChain(fields[1].Declaration.Type, model, ns1);
        Utilities.FillNamespaceChain(fields[2].Declaration.Type, model, ns2);
        Utilities.FillNamespaceChain(fields[3].Declaration.Type, model, ns3);
        Utilities.FillNamespaceChain(fields[4].Declaration.Type, model, ns4);
        Utilities.FillNamespaceChain(fields[5].Declaration.Type, model, ns5);
        Utilities.FillNamespaceChain(fields[6].Declaration.Type, model, ns6);
        Utilities.FillNamespaceChain(fields[7].Declaration.Type, model, ns7);
        Utilities.FillNamespaceChain(fields[8].Declaration.Type, model, ns8);
        Utilities.FillNamespaceChain(fields[9].Declaration.Type, model, ns9);

        Assert.IsTrue(ns0.SequenceEqual(new[] { "NamespaceA1" }));
        Assert.IsTrue(ns1.SequenceEqual(new[] { "NamespaceB", "NamespaceA1" }));
        Assert.IsTrue(ns2.SequenceEqual(new[] { "NamespaceA1", "NamespaceA1.NamespaceA2" }));
        Assert.IsTrue(ns3.SequenceEqual(new[] { "NamespaceA1", "NamespaceA1.NamespaceA2" }));
        Assert.IsTrue(ns4.SequenceEqual(new[] { "NamespaceA1", "NamespaceA1.NamespaceA2" }));
        Assert.IsTrue(ns5.SequenceEqual(new[] { "NamespaceB", "NamespaceA1", "NamespaceA1.NamespaceA2" }));
        Assert.IsTrue(ns6.Count == 0);
        Assert.IsTrue(ns7.Count == 0);
        Assert.IsTrue(ns8.SequenceEqual(new[] { "NamespaceA1", "NamespaceA1.NamespaceA2" }));
        Assert.IsTrue(ns9.SequenceEqual(new[] { "NamespaceA1" }));
    }

    [TestMethod]
    public void GetBaseClassInfo1()
    {
        SourceText sourceText = SourceText.From(@"
namespace SomeNamespace
{
    public interface IFace { }
    public partial class SomeClass : Intermediate, IFace { }
    public partial class Intermediate : Base, IFace {}
    public partial class Base : IFace
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void SomethingOnPropertyChangedXYZ([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) { }
    }
}", Encoding.UTF8);

        var tree = CSharpSyntaxTree.ParseText(sourceText);
        if (tree == null) { Assert.Fail("Tree is null"); return; }

        var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
        var model = compilation.GetSemanticModel(tree);
        if (model == null) { Assert.Fail(); return; }

        var someClassDeclaration = tree.GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ToString() == "SomeClass");
        if (someClassDeclaration == null) { Assert.Fail(); return; }

        var (generateEvent, triggerName) = someClassDeclaration.GetPropertyChangedGenerationInfo(model);
        Assert.IsFalse(generateEvent);
        Assert.AreEqual("SomethingOnPropertyChangedXYZ", triggerName);
    }


    [TestMethod]
    public void GetBaseClassInfo2()
    {
        SourceText sourceText = SourceText.From(@"
namespace SomeNamespace
{
    public interface IFace { }
    public partial class SomeClass : Intermediate, IFace { }
    public partial class Intermediate : Base, IFace {}
    public partial class Base : IFace
    {
    }
}", Encoding.UTF8);

        var tree = CSharpSyntaxTree.ParseText(sourceText);
        if (tree == null) { Assert.Fail("Tree is null"); return; }

        var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
        var model = compilation.GetSemanticModel(tree);
        if (model == null) { Assert.Fail(); return; }

        var someClassDeclaration = tree.GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ToString() == "SomeClass");
        if (someClassDeclaration == null) { Assert.Fail(); return; }

        var (generateEvent, triggerName) = someClassDeclaration.GetPropertyChangedGenerationInfo(model);
        Assert.IsTrue(generateEvent);
        Assert.AreEqual("OnPropertyChanged", triggerName);
    }






    private (IList<FieldDeclarationSyntax> fields, SemanticModel? model) GetFields(SourceText sourceText)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        if (tree == null)
        {
            Assert.Fail("Tree is null");
            return (new List<FieldDeclarationSyntax>(), null);
        }

        var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
        var model = compilation.GetSemanticModel(tree);

        var fields = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .ToList();

        return (fields, model);
    }
}


