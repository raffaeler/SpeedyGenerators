using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
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

            var (fields, model) = GetFields(sourceText);

            if (model == null) { Assert.Fail(); return; }
            var declaration = fields.Single().Declaration;

            HashSet<string> ns = Utilities.GetNamespaceChain(declaration.Type, model);

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

            HashSet<string> ns0 = Utilities.GetNamespaceChain(fields[0].Declaration.Type, model);
            HashSet<string> ns1 = Utilities.GetNamespaceChain(fields[1].Declaration.Type, model);
            HashSet<string> ns2 = Utilities.GetNamespaceChain(fields[2].Declaration.Type, model);
            HashSet<string> ns3 = Utilities.GetNamespaceChain(fields[3].Declaration.Type, model);
            HashSet<string> ns4 = Utilities.GetNamespaceChain(fields[4].Declaration.Type, model);
            HashSet<string> ns5 = Utilities.GetNamespaceChain(fields[5].Declaration.Type, model);
            HashSet<string> ns6 = Utilities.GetNamespaceChain(fields[6].Declaration.Type, model);
            HashSet<string> ns7 = Utilities.GetNamespaceChain(fields[7].Declaration.Type, model);
            HashSet<string> ns8 = Utilities.GetNamespaceChain(fields[8].Declaration.Type, model);
            HashSet<string> ns9 = Utilities.GetNamespaceChain(fields[9].Declaration.Type, model);
            if (ns0 == null || ns1 == null || ns2 == null || ns3 == null
                || ns4 == null || ns5 == null || ns6 == null || ns7 == null
                || ns8 == null || ns9 == null) { Assert.Fail(); return; }

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


        private string Normalize(string input)
        => input.Replace("\r\n", "\n");

        private (IList<FieldDeclarationSyntax> fields, SemanticModel? model) GetFields(SourceText sourceText)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceText);
            if (tree == null) { Assert.Fail("Tree is null"); return (new List<FieldDeclarationSyntax>(), null); }

            var compilation = CSharpCompilation.Create("fake", new[] { tree }, null, null);
            var model = compilation.GetSemanticModel(tree);

            var fields = tree.GetRoot()
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .ToList();

            return (fields, model);
        }
    }
}


