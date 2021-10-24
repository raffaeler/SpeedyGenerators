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
                    false).ToString();

            var expected = @"public string Status
{
    get => _status;
    set
    {
        var oldValue = _status;
        _status = value;
        OnPropertyChanged();
        OnStatusChanged(oldValue, _status);
    }
}";
            Assert.AreEqual(expected, statement);
        }


    }
}