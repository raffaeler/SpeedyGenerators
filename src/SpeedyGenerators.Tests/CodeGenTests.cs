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
public class CodeGenTests
{
    [TestMethod]
    public void CreateCompareValueAndReturn()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreateCompareValueAndReturn("_field");
        Assert.AreEqual("if (_field == value)\r\n    return;", statement.ToString());
    }

    [TestMethod]
    public void CreateCallOnPropChanged()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreateCallMethod("OnPropertyChanged");
        Assert.AreEqual("OnPropertyChanged();", statement.ToString());
    }

    [TestMethod]
    public void CreateSetFieldValue()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreateSetFieldValue("_field");
        Assert.AreEqual("_field = value;", statement.ToString());
    }

    [TestMethod]
    public void CreateDeclareLocalOldValue()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreateDeclareLocalOldValue("_field", "oldValue");
        Assert.AreEqual("var oldValue = _field;", statement.ToString());
    }

    [TestMethod]
    public void CreateCallMethod2()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreateCallMethod("OnFieldChanged", "oldValue", "_field");
        Assert.AreEqual("OnFieldChanged(oldValue, _field);", statement.ToString());
    }

    [TestMethod]
    public void CreatePropertyWithPropertyChanged()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
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
        var expected = """
            public string Status
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
                    OnOnePropertyHasChanged("Status");
                }
            }
            """;
        Assert.AreEqual(Normalize(expected), Normalize(statement.ToString()));
    }

    [TestMethod]
    public void CreateGlobalPartialMethod()
    {
        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var globalPartialmethodParameters = mgr.CreateParameters((mgr.GetTypeName("string"), "propertyName"));
        var statement = mgr.CreatePartialMethod(Array.Empty<string>(), "OnOnePropertyHasChanged", mgr.GetVoidTypeName(), globalPartialmethodParameters)
            .NormalizeWhitespace();

        // ToString does not include the leading trivias (comments)
        var expected = @"partial void OnOnePropertyHasChanged(string propertyName);";
        Assert.AreEqual(Normalize(expected), Normalize(statement.ToString()));
    }

    private string Normalize(string input)
        => input.Replace("\r\n", "\n");

    [TestMethod]
    public void CreateInitializingConstructor()
    {
        string expected = """

                /// <summary>
                /// Test1
                /// Test2
                /// </summary>
                public FakeClass(int id, string name)
                {
                    Id = id;
                    Name = name;
                }
                """;

        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var ctor = mgr.CreateConstructorInitializingProperties(
            new[] { "Test1", "Test2" },
            (mgr.GetTypeName("int"), "Id"),
            (mgr.GetTypeName("string"), "Name"))
            .NormalizeWhitespace();

        // ToString does not include the leading trivias (comments)
        Assert.AreEqual(Normalize(expected), Normalize(ctor.ToFullString()));
    }

    [TestMethod]
    public void CreatePropertyNoSetter()
    {
        var expected = """

            /// <summary>
            /// Test1
            /// Test2
            /// </summary>
            public string Name { get; }
            """;

        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreatePropertyWithInitializer(
                new[] { "Test1", "Test2" },
                mgr.GetTypeName("string"),
                "Name",
                null,
                createSetter: false,
                createPublicSetter: true,
                isOverride: false);

        // ToString does not include the leading trivias (comments)
        Assert.AreEqual(Normalize(expected), Normalize(statement.ToFullString()));
    }

    [TestMethod]
    public void CreatePropertySetterPublic()
    {
        var expected = """

            /// <summary>
            /// Test1
            /// Test2
            /// </summary>
            public string Name { get; set; }
            """;

        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreatePropertyWithInitializer(
                new[] { "Test1", "Test2" },
                mgr.GetTypeName("string"),
                "Name",
                null,
                createSetter: true,
                createPublicSetter: true,
                isOverride: false);

        // ToString does not include the leading trivias (comments)
        Assert.AreEqual(Normalize(expected), Normalize(statement.ToFullString()));
    }

    [TestMethod]
    public void CreatePropertySetterPrivate()
    {
        var expected = """

            /// <summary>
            /// Test1
            /// Test2
            /// </summary>
            public string Name { get; private set; }
            """;

        var mgr = new ConcreteTypeGenerator("FakeNS", "FakeClass");
        var statement = mgr.CreatePropertyWithInitializer(
                new[] { "Test1", "Test2" },
                mgr.GetTypeName("string"),
                "Name",
                null,
                createSetter: true,
                createPublicSetter: false,
                isOverride: false);

        // ToString does not include the leading trivias (comments)
        Assert.AreEqual(Normalize(expected), Normalize(statement.ToFullString()));
    }




}


