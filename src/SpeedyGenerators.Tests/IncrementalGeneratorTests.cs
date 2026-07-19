using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpeedyGenerators;

namespace SpeedyGenerators.Tests;

[TestClass]
public class IncrementalGeneratorTests
{
    private static ImmutableArray<MetadataReference> GetDefaultReferences()
    {
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        return trustedAssemblies
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToImmutableArray();
    }

    private static Compilation CreateCompilation(params (string fileName, string source)[] sources)
    {
        var syntaxTrees = sources
            .Select(s => CSharpSyntaxTree.ParseText(SourceText.From(s.source, Encoding.UTF8),
                path: s.fileName))
            .ToArray();

        return CSharpCompilation.Create("TestAssembly",
            syntaxTrees,
            GetDefaultReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriverRunResult RunGenerators(Compilation compilation,
        params IIncrementalGenerator[] generators)
    {
        var driver = CSharpGeneratorDriver.Create(
            generators: generators.Select(g => g.AsSourceGenerator()),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            optionsProvider: null);

        return driver.RunGenerators(compilation).GetRunResult();
    }

    [TestMethod]
    public void AttributeClassGenerator_GeneratesAttributes()
    {
        var compilation = CreateCompilation(
            ("Empty.cs", "namespace Test { }"));

        var result = RunGenerators(compilation, new AttributeClassGenerator());

        Assert.AreEqual(1, result.GeneratedTrees.Length,
            "Should generate exactly one source file");
        var generatedSource = result.GeneratedTrees[0].ToString();

        Assert.IsTrue(generatedSource.Contains("class MakePropertyAttribute"),
            "Should contain MakePropertyAttribute class");
        Assert.IsTrue(generatedSource.Contains("class MakeConcreteAttribute"),
            "Should contain MakeConcreteAttribute class");
        Assert.IsTrue(generatedSource.Contains("namespace SpeedyGenerators"),
            "Should be in SpeedyGenerators namespace");
    }

    [TestMethod]
    public void PropertyChangedGenerator_BasicGeneration()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public partial class TestClass
    {
        [MakeProperty(""Name"")]
        private string _name;
    }
}";

        var compilation = CreateCompilation(
            ("Test.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new PropertyChangedGenerator());

        // Find the generated file for TestClass
        var generatedTrees = result.GeneratedTrees
            .Where(t => t.FilePath.Contains("TestClass"))
            .ToArray();

        Assert.IsTrue(generatedTrees.Length > 0,
            "Should generate a file for TestClass");

        var generatedSource = generatedTrees[0].ToString();

        Assert.IsTrue(generatedSource.Contains("INotifyPropertyChanged"),
            "Should implement INotifyPropertyChanged");
        Assert.IsTrue(generatedSource.Contains("public string Name"),
            "Should contain the Name property");
        Assert.IsTrue(generatedSource.Contains("get => _name"),
            "Should contain the getter");
        Assert.IsTrue(generatedSource.Contains("OnPropertyChanged();"),
            "Should call OnPropertyChanged");
        Assert.IsTrue(generatedSource.Contains("PropertyChangedEventHandler? PropertyChanged"),
            "Should declare PropertyChanged event");
    }

    [TestMethod]
    public void PropertyChangedGenerator_MultipleFields()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public partial class MultiPropClass
    {
        [MakeProperty(""FirstName"")]
        private string _firstName;

        [MakeProperty(""LastName"")]
        private string _lastName;

        [MakeProperty(""Age"")]
        private int _age;
    }
}";

        var compilation = CreateCompilation(
            ("Test.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new PropertyChangedGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiPropClass"));
        Assert.IsNotNull(generatedTree, "Should generate a file for MultiPropClass");

        var source2 = generatedTree.ToString();
        Assert.IsTrue(source2.Contains("public string FirstName"),
            "Should contain FirstName property");
        Assert.IsTrue(source2.Contains("public string LastName"),
            "Should contain LastName property");
        Assert.IsTrue(source2.Contains("public int Age"),
            "Should contain Age property");
    }

    [TestMethod]
    public void PropertyChangedGenerator_ExtraNotify()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public partial class ExtraNotifyClass
    {
        [MakeProperty(""Status"", true)]
        private string _status;
    }
}";

        var compilation = CreateCompilation(
            ("Test.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new PropertyChangedGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ExtraNotifyClass"));
        Assert.IsNotNull(generatedTree);

        var src = generatedTree.ToString();
        Assert.IsTrue(src.Contains("OnStatusChanged"),
            "Should contain OnStatusChanged partial method");
        Assert.IsTrue(src.Contains("partial void OnStatusChanged"),
            "Should declare the partial method");
    }

    [TestMethod]
    public void PropertyChangedGenerator_CompareValues()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public partial class CompareValuesClass
    {
        [MakeProperty(""Value"", false, true)]
        private int _value;
    }
}";

        var compilation = CreateCompilation(
            ("Test.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new PropertyChangedGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CompareValuesClass"));
        Assert.IsNotNull(generatedTree);

        var src = generatedTree.ToString();
        Assert.IsTrue(src.Contains("if (_value == value)"),
            "Should contain value comparison");
        Assert.IsTrue(src.Contains("return;"),
            "Should return early if equal");
    }

    [TestMethod]
    public void MakeConcreteGenerator_BasicGeneration()
    {
        var source = @"
using SpeedyGenerators;

namespace TestNs
{
    public interface IMyService
    {
        int Id { get; }
        string Name { get; set; }
    }

    [MakeConcrete(""TestNs.IMyService"")]
    public partial class MyMock
    {
    }
}";

        var compilation = CreateCompilation(
            ("MyService.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new MakeConcreteGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyMock"));
        Assert.IsNotNull(generatedTree, "Should generate a file for MyMock");

        var src = generatedTree.ToString();
        Assert.IsTrue(src.Contains("public int Id"),
            "Should contain Id property");
        Assert.IsTrue(src.Contains("public string Name"),
            "Should contain Name property");
    }

    [TestMethod]
    public void MakeConcreteGenerator_WithConstructor()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public interface IRepo
    {
        string ConnectionString { get; }
        int Timeout { get; }
    }

    [MakeConcrete(""TestNs.IRepo"", true)]
    public partial class RepoMock
    {
    }
}";

        var compilation = CreateCompilation(
            ("Repo.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new MakeConcreteGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("RepoMock"));
        Assert.IsNotNull(generatedTree);

        var src = generatedTree.ToString();
        Assert.IsTrue(src.Contains("public RepoMock"),
            "Should contain a constructor");
        Assert.IsTrue(src.Contains("ConnectionString = connectionString"),
            "Constructor should initialize properties");
        Assert.IsTrue(src.Contains("Timeout = timeout"),
            "Constructor should initialize all properties");
    }

    [TestMethod]
    public void MakeConcreteGenerator_PrivateSetters()
    {
        var source = @"
using SpeedyGenerators;
namespace TestNs
{
    public interface IConfig
    {
        string Key { get; set; }
    }

    [MakeConcrete(""TestNs.IConfig"", true, true)]
    public partial class ConfigMock
    {
    }
}";

        var compilation = CreateCompilation(
            ("Config.cs", source),
            ("Attributes.cs", GetAttributeSourceCode()));

        var result = RunGenerators(compilation,
            new AttributeClassGenerator(),
            new MakeConcreteGenerator());

        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ConfigMock"));
        Assert.IsNotNull(generatedTree);

        var src = generatedTree.ToString();
        Assert.IsTrue(src.Contains("private set"),
            "Should have private setter");
    }

    private static string GetAttributeSourceCode() => @"
using System;
namespace SpeedyGenerators
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class MakePropertyAttribute : Attribute
    {
        public MakePropertyAttribute(string name, bool extraNotify = false, bool compareValues = false) { }
        public string Name { get; }
        public bool ExtraNotify { get; }
        public bool CompareValues { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class MakeConcreteAttribute : Attribute
    {
        public MakeConcreteAttribute(string interfaceFullTypeName,
            bool generateInitializingConstructor = true,
            bool makeSettersPrivate = false,
            bool implementInterface = false,
            bool makeReferenceTypesNullable = false,
            bool makeValueTypesNullable = false)
        {
            InterfaceFullTypeName = interfaceFullTypeName;
            GenerateInitializingConstructor = generateInitializingConstructor;
            MakeSettersPrivate = makeSettersPrivate;
            ImplementInterface = implementInterface;
            MakeReferenceTypesNullable = makeReferenceTypesNullable;
            MakeValueTypesNullable = makeValueTypesNullable;
        }

        public string InterfaceFullTypeName { get; }
        public bool GenerateInitializingConstructor { get; }
        public bool MakeSettersPrivate { get; }
        public bool ImplementInterface { get; }
        public bool MakeReferenceTypesNullable { get; }
        public bool MakeValueTypesNullable { get; }
    }
}";
}
