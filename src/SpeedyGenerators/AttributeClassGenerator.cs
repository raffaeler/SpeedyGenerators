using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SpeedyGenerators
{
    [Generator]
    public class AttributeClassGenerator : ISourceGenerator
    {
        private static readonly string _attributeNamespace = "SpeedyGenerators";
        private static readonly string _attributeClassName = "MakePropertyAttribute";

        private static SourceText sourceText = SourceText.From($@"
using System;

namespace {_attributeNamespace}
{{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class MakePropertyAttribute : Attribute
    {{
        public MakePropertyAttribute(string name, bool extraNotify = false, bool compareValues = false)
        {{
            this.Name = name;
            this.ExtraNotify = extraNotify;
            this.CompareValues = compareValues;
        }}

        public string Name {{ get; private set; }}
        public bool ExtraNotify {{ get; private set; }}
        public bool CompareValues {{ get; private set; }}
    }}
}}", Encoding.UTF8);


        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(_attributeClassName, sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}



//namespace SpeedyGenerators
//{
//    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
//    sealed class MakePropertyAttribute : Attribute
//    {
//        public MakePropertyAttribute(string name, bool extraNotify = false, bool compareValues = false)
//        {
//            this.Name = name;
//            this.ExtraNotify = extraNotify;
//            this.CompareValues = compareValues;
//        }

//        public string Name { get; private set; }
//        public bool ExtraNotify { get; private set; }
//        public bool CompareValues { get; private set; }
//    }
//}