using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpeedyGenerators.Tests
{
    [TestClass]
    public class CodeGenTests
    {
        [TestMethod]
        public void CreateCompareValueAndReturn()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreateCompareValueAndReturn("_field");
            Assert.AreEqual("if (_field == value)\r\n    return;", statement.ToString());
        }

        [TestMethod]
        public void CreateCallOnPropChanged()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreateCallOnPropChanged("OnPropertyChanged");
            Assert.AreEqual("OnPropertyChanged();", statement.ToString());
        }

        [TestMethod]
        public void CreateSetFieldValue()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreateSetFieldValue("_field");
            Assert.AreEqual("_field = value;", statement.ToString());
        }

        [TestMethod]
        public void CreateDeclareLocalOldValue()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreateDeclareLocalOldValue("_field", "oldValue");
            Assert.AreEqual("var oldValue = _field;", statement.ToString());
        }

        [TestMethod]
        public void CreateCallMethod2()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreateCallMethod2("OnFieldChanged", "oldValue", "_field");
            Assert.AreEqual("OnFieldChanged(oldValue, _field);", statement.ToString());
        }

        [TestMethod]
        public void CreatePropertyWithPropertyChanged()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var statement = mgr.CreatePropertyWithPropertyChanged(
                    new[] { "line1", "line2" },
                    mgr.GetTypeName("string"),
                    "Status",
                    "_status",
                    "OnPropertyChanged",
                    "OnStatusChanged",
                    true,
                    false);

            // ToString does not include the leading trivias (comments)
            var expected = @"public string Status
{
    get => _status;
    set
    {
        if (_status == value)
            return;
        var oldValue = _status;
        _status = value;
        OnPropertyChanged();
        OnStatusChanged(oldValue, _status);
    }
}";
            Assert.AreEqual(Normalize(expected), Normalize(statement.ToString()));
        }

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

            var tree = CSharpSyntaxTree.ParseText(sourceText);
            if (tree == null) { Assert.Fail("Tree is null"); return; }

            var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
            var model = compilation.GetSemanticModel(tree);

            var field = tree.GetRoot()
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Single();

            var declaration = field.Declaration;

            HashSet<string> ns = Utilities.GetNamespaceChain(declaration.Type, model);

            if (ns == null) { Assert.Fail(); }
            else
            {
                Assert.IsTrue(ns == null
                    ? false
                    : ns.SequenceEqual(new[] { "NamespaceGen", "SomeNamespace" }));
            }
        }


        private string Normalize(string input)
            => input.Replace("\r\n", "\n");
    }
}
