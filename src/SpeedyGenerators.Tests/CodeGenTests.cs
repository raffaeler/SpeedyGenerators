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
            var statement = mgr.CreateCallMethod("OnPropertyChanged");
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
            var statement = mgr.CreateCallMethod("OnFieldChanged", "oldValue", "_field");
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
                    "OnOnePropertyHasChanged",
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
        OnOnePropertyHasChanged(""Status"");
    }
}";
            Assert.AreEqual(Normalize(expected), Normalize(statement.ToString()));
        }

        [TestMethod]
        public void CreateGlobalPartialMethod()
        {
            var mgr = new ClassGenerator("FakeNS", "FakeClass");
            var globalPartialmethodParameters = mgr.CreateParameters((mgr.GetTypeName("string"), "propertyName"));
            var statement = mgr.CreatePartialMethod(Array.Empty<string>(), "OnOnePropertyHasChanged", mgr.GetVoidTypeName(), globalPartialmethodParameters)
                .NormalizeWhitespace();

            // ToString does not include the leading trivias (comments)
            var expected = @"partial void OnOnePropertyHasChanged(string propertyName);";
            Assert.AreEqual(Normalize(expected), Normalize(statement.ToString()));
        }

        private string Normalize(string input)
            => input.Replace("\r\n", "\n");

    }
}


