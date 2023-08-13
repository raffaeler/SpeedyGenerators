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
public class UtilityTests
{
    [TestMethod]
    public void CreateCompareValueAndReturn()
    {
        var expected = "abcDefGhi";
        Assert.AreEqual(expected, "AbcDefGhi".toCamel());
    }

}
