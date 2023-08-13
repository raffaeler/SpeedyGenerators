using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace SpeedyGenerators;

public class ConcreteTypeGenerator
{
    public ConcreteTypeGenerator(string? @namespace, string className)
    {
        this.Namespace = @namespace;
        this.ClassName = className;
    }

    public SyntaxTokenList Modifiers { get; set; }
    public HashSet<string> Usings { get; } = new();
    public string? Namespace { get; }
    public string ClassName { get; }
    public string? BaseClass { get; set; }
    public HashSet<string> Interfaces { get; } = new();
    public bool EnableNullable { get; set; }

    internal List<MemberDeclarationSyntax> Members { get; } = new();

    public SourceText Generate(ConcreteTypeKind concreteTypeKind)
    {
        var compilationUnit = SyntaxFactory.CompilationUnit();

        var usingDirectives = Usings
            .Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(u)))
            .ToArray();
        compilationUnit = compilationUnit.AddUsings(usingDirectives);

        NamespaceDeclarationSyntax? nspaceDeclaration = null;

        if (Namespace != null)
        {
            nspaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.IdentifierName(Namespace));
        }

        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
        if (!Modifiers.Contains(partialToken)) Modifiers.Add(partialToken);

        BaseTypeSyntax[] baseTypes = GetBaseTypes();
        var baseList = baseTypes.Length == 0 ? null : SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(GetBaseTypes()));
        BaseTypeDeclarationSyntax? typeDeclaration = null;
        SyntaxList<AttributeListSyntax> attributeList = SyntaxFactory.List<AttributeListSyntax>();
        SyntaxList<MemberDeclarationSyntax> membersList = SyntaxFactory.List(Members);
        ParameterListSyntax? parametersList = null;// SyntaxFactory.ParameterList();
        typeDeclaration = concreteTypeKind switch
        {
            //ConcreteTypeKind.Class => SyntaxFactory.ClassDeclaration(ClassName).WithMembers(membersList),
            ConcreteTypeKind.Class => SyntaxFactory.ClassDeclaration(
                attributeList,
                Modifiers,
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                SyntaxFactory.Identifier(ClassName),
                null,
                baseList,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                membersList,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

            //ConcreteTypeKind.Struct => SyntaxFactory.StructDeclaration(ClassName).WithMembers(membersList),
            ConcreteTypeKind.Struct => SyntaxFactory.StructDeclaration(
                attributeList,
                Modifiers,
                SyntaxFactory.Token(SyntaxKind.StructKeyword),
                SyntaxFactory.Identifier(ClassName),
                null,
                baseList,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                membersList,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

            ConcreteTypeKind.RecordClass => SyntaxFactory.RecordDeclaration(
                SyntaxKind.RecordDeclaration,
                attributeList,
                Modifiers,
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                SyntaxFactory.Identifier(ClassName),
                null,
                parametersList,
                baseList,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                membersList,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),


            ConcreteTypeKind.RecordStruct => SyntaxFactory.RecordDeclaration(
                SyntaxKind.RecordStructDeclaration,
                attributeList,
                Modifiers,
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                SyntaxFactory.Token(SyntaxKind.StructKeyword),
                SyntaxFactory.Identifier(ClassName),
                null,
                parametersList,
                baseList,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                membersList,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

            _ => throw new Exception("Invalid ConcreteTypeKind")
        };

        //typeDeclaration = typeDeclaration.WithModifiers(Modifiers);
        //typeDeclaration = AddBaseTypes(typeDeclaration);

        if (nspaceDeclaration != null)
        {
            nspaceDeclaration = nspaceDeclaration.AddMembers(typeDeclaration);
            if (EnableNullable)
            {
                nspaceDeclaration =
                    nspaceDeclaration.WithLeadingTrivia(CreateNullableEnable());
            }

            compilationUnit = compilationUnit.AddMembers(nspaceDeclaration);
        }
        else
        {
            if (EnableNullable)
            {
                typeDeclaration = typeDeclaration.WithLeadingTrivia(CreateNullableEnable());
            }

            compilationUnit = compilationUnit.AddMembers(typeDeclaration);
        }

        if (EnableNullable)
        {
            compilationUnit = compilationUnit.WithEndOfFileToken(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(
                        CreateNullableRestore()),
                    SyntaxKind.EndOfFileToken,
                    SyntaxFactory.TriviaList()));
        }

        return SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8);
    }

    private BaseTypeSyntax[] GetBaseTypes()
    {
        List<string> bases = new();
        if (!string.IsNullOrEmpty(BaseClass))
        {
#pragma warning disable CS8604 // Possible null reference argument.
            bases.Add(BaseClass);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        if (Interfaces.Count > 0)
        {
            bases.AddRange(Interfaces);
        }

        var identifiers = bases
            .Select(b => SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(b)))
            .ToArray();

        return identifiers;
    }

    private BaseTypeDeclarationSyntax AddBaseTypes(BaseTypeDeclarationSyntax declaration)
    {
        var identifiers = GetBaseTypes();

        if (identifiers.Length > 0)
        {
            declaration = declaration.AddBaseListTypes(identifiers);
        }

        return declaration;
    }

    // valid for ClassDeclarationSyntax, StructDeclarationSyntax and RecordDeclarationSyntax
    internal TypeDeclarationSyntax AddSerializableAttribute(TypeDeclarationSyntax classDeclaration)
    {
        classDeclaration = classDeclaration.AddAttributeLists(
        SyntaxFactory.AttributeList(
            SyntaxFactory.SeparatedList<AttributeSyntax>(
                new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Serializable"))
        })));

        return classDeclaration;
    }

    internal ExpressionSyntax CreateInitializersWithStrings(string type, params string[] arguments)
    {
        var typeName = SyntaxFactory.ParseTypeName(type);
        var argumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(arguments
                .Select(a =>
                    SyntaxFactory.Argument(CreateStringLiteralExpression(a)))));

        var initializer = SyntaxFactory.ObjectCreationExpression(typeName, argumentList, null);
        return initializer;
    }

    internal ExpressionSyntax CreateInitializersWithExpressions(string type, params ExpressionSyntax[] arguments)
    {
        var typeName = SyntaxFactory.ParseTypeName(type);
        var argumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(arguments
                .Select(a => SyntaxFactory.Argument(a))));

        var initializer = SyntaxFactory.ObjectCreationExpression(typeName, argumentList, null);
        return initializer;
    }

    internal ExpressionSyntax CreateStringLiteralExpression(string literal)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(literal));
    }

    internal ExpressionSyntax CreateNumericLiteralExpression(int number)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(number));
    }

    internal ExpressionSyntax CreateMemberAccess2(string left, string right)
    {
        return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(left),
                    SyntaxFactory.IdentifierName(right));
    }

    internal ExpressionSyntax CreateTuple(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.TupleExpression(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]{
                            SyntaxFactory.Argument(left),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(right)
                        }));
    }

    internal ExpressionSyntax CreateCreateObject(string typeName, params ExpressionSyntax[] arguments)
    {
        List<SyntaxNodeOrToken> nodes = new();
        bool isFirst = true;
        foreach (var arg in arguments)
        {
            if (isFirst)
                isFirst = false;
            else
                nodes.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            nodes.Add(SyntaxFactory.Argument(arg));

        }

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(typeName))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(nodes))).NormalizeWhitespace();
    }

    internal ExpressionSyntax CreateInitializerWithCollection(string typeName, params ExpressionSyntax[] arguments)
    {
        List<SyntaxNodeOrToken> argumentList = new();
        bool isFirst = true;
        foreach (var arg in arguments)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                //argumentList.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                argumentList.Add(SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(),
                        SyntaxKind.CommaToken,
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.CarriageReturnLineFeed)));
            }

            argumentList.Add(arg);
        }

        var initializer = SyntaxFactory.ImplicitObjectCreationExpression()
            .WithInitializer(
                SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(argumentList)));
        return initializer;
    }

    public SyntaxToken CreateModifier(SyntaxKind kind, IEnumerable<string> commentLines)
    {
        if (commentLines.Any())
        {
            SyntaxTrivia comment = CreateXmlComment(true, commentLines);
            return SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(comment),
                    kind,
                    SyntaxFactory.TriviaList());
        }

        return SyntaxFactory.Token(kind);
    }

    /// <summary>
    /// </summary>
    internal FieldDeclarationSyntax CreateField(IEnumerable<string> commentLines,
        string type, string variableName, ExpressionSyntax? initializer,
        bool isPrivate, bool isStatic)
    {
        SyntaxKind visibility = isPrivate ? SyntaxKind.PrivateKeyword : SyntaxKind.PublicKeyword;

        var modifier = CreateModifier(visibility, commentLines);

        var typeName = SyntaxFactory.ParseTypeName(type);

        var declarator = initializer == null
            ? SyntaxFactory.VariableDeclarator(variableName)
            : SyntaxFactory.VariableDeclarator(variableName)
                .WithInitializer(SyntaxFactory.EqualsValueClause(initializer));

        var variableDeclaration = SyntaxFactory.VariableDeclaration(typeName)
            .AddVariables(declarator);

        var field = SyntaxFactory.FieldDeclaration(variableDeclaration)
            .WithModifiers(SyntaxFactory.TokenList(modifier))
            ;
        if (isStatic)
        {
            field = field
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        return field;
    }

    internal EventFieldDeclarationSyntax CreateEventField(IEnumerable<string> commentLines,
        string delegateType, string variableName, bool isPrivate, bool isStatic)
    {
        SyntaxKind visibility = isPrivate ? SyntaxKind.PrivateKeyword : SyntaxKind.PublicKeyword;

        var modifier = CreateModifier(visibility, commentLines);

        var typeName = SyntaxFactory.ParseTypeName(delegateType);

        var variableDeclaration = SyntaxFactory.VariableDeclaration(typeName)
            .AddVariables(SyntaxFactory.VariableDeclarator(variableName)
            );

        var field = SyntaxFactory.EventFieldDeclaration(variableDeclaration)
            .WithModifiers(SyntaxFactory.TokenList(modifier));
        if (isStatic)
        {
            field = field
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        return field;
    }


    internal GenericNameSyntax MakeListOfT(string genericType)
    {
        return SyntaxFactory.GenericName(SyntaxFactory.Identifier("List"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                        SyntaxFactory.IdentifierName(genericType))));
    }

    //internal SyntaxNode CreateProperty(string[] commentLines, NameSyntax typeName, string propertyName)
    //{
    //    var propertyDeclaration = SyntaxFactory.PropertyDeclaration(typeName,
    //        SyntaxFactory.Identifier(propertyName));




    //    var propertyDeclaration = _generator.PropertyDeclaration(name, type,
    //        Accessibility.Public, DeclarationModifiers.None);

    //    var getAccessor = _generator.GetAccessor(propertyDeclaration,
    //        DeclarationKind.GetAccessor);
    //    var simpleGetAccessor = _generator.WithStatements(getAccessor, null);
    //    propertyDeclaration = _generator.ReplaceNode(propertyDeclaration,
    //        getAccessor, simpleGetAccessor);

    //    var setAccessor = _generator.GetAccessor(propertyDeclaration,
    //        DeclarationKind.SetAccessor);
    //    var simpleSetAccessor = _generator.WithStatements(setAccessor, null);
    //    propertyDeclaration = _generator.ReplaceNode(propertyDeclaration,
    //        setAccessor, simpleSetAccessor);

    //    return propertyDeclaration;
    //}

    internal PropertyDeclarationSyntax CreatePropertyWithInitializer(string[] commentLines,
        TypeSyntax typeName, string propertyName, ExpressionSyntax? initializer,
        bool createSetter = false, bool createPublicSetter = false, bool isOverride = false)
    {
        //var type2 = SyntaxFactory.IdentifierName(
        //    SyntaxFactory.Identifier(
        //        SyntaxFactory.TriviaList(comment),
        //            "int", SyntaxFactory.TriviaList()));

        //typeName = typeName.(comment);

        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(typeName,
            SyntaxFactory.Identifier(propertyName));

        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PublicKeyword, commentLines));
        if (isOverride) modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));

        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var setterModifiers = createPublicSetter
            ? SyntaxFactory.TokenList()
            : SyntaxFactory.TokenList(CreateModifier(SyntaxKind.PrivateKeyword, Array.Empty<string>()));

        var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
            SyntaxFactory.List<AttributeListSyntax>(),
            setterModifiers,
            null,
            null)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var list = !createSetter
            ? SyntaxFactory.SingletonList<AccessorDeclarationSyntax>(getter)
            : SyntaxFactory.List<AccessorDeclarationSyntax>(new[] { getter, setter });

        propertyDeclaration = propertyDeclaration
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithAccessorList(SyntaxFactory.AccessorList(list));

        if (initializer != null)
        {
            propertyDeclaration = propertyDeclaration
            .WithInitializer(SyntaxFactory.EqualsValueClause(initializer));

            propertyDeclaration = propertyDeclaration
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        //propertyDeclaration = propertyDeclaration
        //    .WithLeadingTrivia(comment);

        return propertyDeclaration.NormalizeWhitespace();
    }

    internal PropertyDeclarationSyntax CreatePropertyWithArrowCallingBase(string[] commentLines,
        NameSyntax typeName, string propertyName, bool isOverride = false)
    {
        //var type2 = SyntaxFactory.IdentifierName(
        //    SyntaxFactory.Identifier(
        //        SyntaxFactory.TriviaList(comment),
        //            "int", SyntaxFactory.TriviaList()));

        //typeName = typeName.(comment);

        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(typeName,
            SyntaxFactory.Identifier(propertyName))
            ;

        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PublicKeyword, commentLines));
        if (isOverride) modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));

        //  => base.Links;
        ArrowExpressionClauseSyntax arrowClause =
            SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.BaseExpression(),
                    SyntaxFactory.IdentifierName(propertyName)));

        propertyDeclaration = propertyDeclaration
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithExpressionBody(arrowClause)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        //propertyDeclaration = propertyDeclaration
        //    .WithLeadingTrivia(comment);

        return propertyDeclaration.NormalizeWhitespace();
    }

    internal PropertyDeclarationSyntax CreatePropertyWithPropertyChanged(
        string[] commentLines, TypeSyntax typeSyntax, string propertyName, string fieldName,
        string propertyChangedMethod, string? partialMethodName, string? globalPartialMethodName, bool compareValues,
        bool isOverride = false)
    {
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(typeSyntax,
            SyntaxFactory.Identifier(propertyName));

        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PublicKeyword, commentLines));
        if (isOverride) modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));

        var localOld = "oldValue";
        List<StatementSyntax> setterStatements = new();
        if (compareValues)
        {
            // if (_field == value) return;
            setterStatements.Add(CreateCompareValueAndReturn(fieldName));
        }

        if (!string.IsNullOrEmpty(partialMethodName))
        {
            // var old = _field;
            setterStatements.Add(CreateDeclareLocalOldValue(fieldName, localOld));
        }

        // _field = value;
        setterStatements.Add(CreateSetFieldValue(fieldName));

        // OnPropertyChanged();
        setterStatements.Add(CreateCallMethod(propertyChangedMethod));

        if (!string.IsNullOrEmpty(partialMethodName))
        {
            // OnFieldChanged(old, _field);
#pragma warning disable CS8604 // Possible null reference argument.
            setterStatements.Add(CreateCallMethod(partialMethodName, localOld, fieldName));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        if (!string.IsNullOrEmpty(globalPartialMethodName))
        {
#pragma warning disable CS8604 // Possible null reference argument.
            setterStatements.Add(CreateCallMethod(globalPartialMethodName, CreateStringLiteralExpression(propertyName)));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        var getterBody = SyntaxFactory.ArrowExpressionClause(
            SyntaxFactory.IdentifierName(fieldName));

        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithExpressionBody(getterBody)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var setterBody = SyntaxFactory.Block(setterStatements);
        var setter = SyntaxFactory.AccessorDeclaration(
            SyntaxKind.SetAccessorDeclaration, setterBody);


        propertyDeclaration = propertyDeclaration
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithAccessorList(SyntaxFactory.AccessorList(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.List(new[] { getter, setter, }),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)));

        return propertyDeclaration.NormalizeWhitespace();
    }

    public ExpressionStatementSyntax CreateSetFieldValue(string fieldName)
        => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName("value"))).NormalizeWhitespace();

    /// <summary>
    /// if (_field == value) return;
    /// </summary>
    public StatementSyntax CreateCompareValueAndReturn(string fieldName)
    {
        return
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName("value")),
                SyntaxFactory.ReturnStatement()).NormalizeWhitespace();
    }

    // var old = _field;
    public LocalDeclarationStatementSyntax CreateDeclareLocalOldValue(string fieldName,
        string localVar)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                CreateVarKeyword(),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localVar))
                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.IdentifierName(fieldName)))))).NormalizeWhitespace();

    }

    public ExpressionStatementSyntax CreateCallMethod(string methodName)
        => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(methodName)));

    // MethodName(arg1, arg2);
    public ExpressionStatementSyntax CreateCallMethod(string methodName, params string[] argNames)
    {
        return CreateCallMethod(methodName, argNames
            .Select(a => SyntaxFactory.IdentifierName(a))
            .ToArray());
    }

    // MethodName(arg1, arg2, ...);
    public ExpressionStatementSyntax CreateCallMethod(string methodName, params ExpressionSyntax[] arguments)
    {
        var argumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(arguments.Select(a => SyntaxFactory.Argument(a))));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(methodName), argumentList)).NormalizeWhitespace();
    }

    public IdentifierNameSyntax CreateVarKeyword()
        => SyntaxFactory.IdentifierName(
            SyntaxFactory.Identifier(SyntaxFactory.TriviaList(),
                SyntaxKind.VarKeyword, "var", "var", SyntaxFactory.TriviaList()));

    public ConstructorDeclarationSyntax CreateConstructorInitializingProperties(string[] commentLines,
        params (TypeSyntax type, string name)[] propertiesToInitialize)
    {
        List<ParameterSyntax> parameters = new();
        List<StatementSyntax> statements = new();
        foreach ((TypeSyntax type, string name) tuple in propertiesToInitialize)
        {
            var arg = tuple.name.toCamel();
            if (arg == tuple.name)
            {
                arg += "_value";
            }

            var parameter = SyntaxFactory.Parameter(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                tuple.type,
                SyntaxFactory.Identifier(arg), null);
            parameters.Add(parameter);

            var statement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(tuple.name),
                SyntaxFactory.IdentifierName(arg))).NormalizeWhitespace();
            statements.Add(statement);
        }

        var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));


        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PublicKeyword, commentLines));

        var methodDeclaration = SyntaxFactory.ConstructorDeclaration(this.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithParameterList(parameterList)
            .WithBody(SyntaxFactory.Block(SyntaxFactory.List<StatementSyntax>(statements)));

        return methodDeclaration;
    }

    public ConstructorDeclarationSyntax CreateConstructor(string[] commentLines,
        params StatementSyntax[] statements)
    {
        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PublicKeyword, commentLines));

        var methodDeclaration = SyntaxFactory.ConstructorDeclaration(this.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithBody(SyntaxFactory.Block(SyntaxFactory.List<StatementSyntax>(statements)));

        return methodDeclaration;
    }


    //public SyntaxTrivia CreateXmlComment(bool prependSpace, params string[] lines)
    //{
    //    return CreateXmlComment(prependSpace, lines);
    //}

    public SyntaxTrivia CreateXmlComment(bool prependSpace, IEnumerable<string> lines)
    {
        var tokens = new List<SyntaxToken>();
        tokens.Add(SyntaxFactory.XmlTextNewLine(Utilities.NewLine));

        if (lines != null)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                tokens.Add(SyntaxFactory.XmlTextLiteral(line.StartsWith(" ") ? line : " " + line));
                tokens.Add(SyntaxFactory.XmlTextNewLine(Utilities.NewLine));
            }
        }

        tokens.Add(SyntaxFactory.XmlTextLiteral(" "));

        var xmlnodes = new List<XmlNodeSyntax>();
        if (prependSpace)
        {
            xmlnodes.Add(SyntaxFactory.XmlText()
                .WithTextTokens(SyntaxFactory.TokenList(
                    SyntaxFactory.XmlTextNewLine(SyntaxFactory.TriviaList(),
                                                        Utilities.NewLine,
                                                        Utilities.NewLine,
                                                        SyntaxFactory.TriviaList()))));
        }

        xmlnodes.AddRange(new XmlNodeSyntax[]
        {

            SyntaxFactory.XmlText().WithTextTokens(
                SyntaxFactory.TokenList(
                    SyntaxFactory.XmlTextLiteral(
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.DocumentationCommentExterior("///")),
                        " ",
                        " ",
                        SyntaxFactory.TriviaList()))),

            SyntaxFactory.XmlExampleElement(
                SyntaxFactory.SingletonList<XmlNodeSyntax>(
                    SyntaxFactory.XmlText()
                    .WithTextTokens(
                        SyntaxFactory.TokenList(tokens))))
            .WithStartTag(
                SyntaxFactory.XmlElementStartTag(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier("summary"))))
            .WithEndTag(
                SyntaxFactory.XmlElementEndTag(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier("summary")))),

            SyntaxFactory.XmlText()
                .WithTextTokens(SyntaxFactory.TokenList(
                    SyntaxFactory.XmlTextNewLine(SyntaxFactory.TriviaList(),
                                                        Utilities.NewLine,
                                                        Utilities.NewLine,
                                                        SyntaxFactory.TriviaList()))),
        });

        return SyntaxFactory.Trivia(
                    SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SyntaxFactory.List<XmlNodeSyntax>(xmlnodes)));
    }


    public StatementSyntax CreateAddCollection(string collection, ExpressionSyntax expression)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(collection),
                    SyntaxFactory.IdentifierName("Add")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            expression)))));
    }

    public StatementSyntax CreateSimpleAssignment(string variableName, string stringLiteral)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(variableName),
            CreateStringLiteralExpression(stringLiteral)));
    }

    public MethodDeclarationSyntax CreatePartialMethod(IEnumerable<string> commentLines,
        string partialMethodName, TypeSyntax returnType, IEnumerable<ParameterSyntax> parameters)
    {
        List<SyntaxToken> modifiers = new();
        modifiers.Add(CreateModifier(SyntaxKind.PartialKeyword, commentLines));

        var declaration = SyntaxFactory.MethodDeclaration(returnType, partialMethodName)
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(parameters)))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return declaration;
    }

    public IEnumerable<ParameterSyntax> CreateParameters(params (TypeSyntax typeSyntax, string argName)[] arguments)
    {
        return arguments
            .Select(p =>
                SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier(p.argName))
                    .WithType(p.typeSyntax))
            .ToArray();
    }

    public ParameterSyntax CreateParameterForCallerMemberName(string parameterName)
    {
        // [CallerMemberName] string parameterName = null
        // where 'parameterName' is whatever (non-validated) string
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
            .WithAttributeLists(SyntaxFactory.SingletonList(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("CallerMemberName"))))))
            .WithType(SyntaxFactory.NullableType(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.StringKeyword))))
            .WithDefault(SyntaxFactory.EqualsValueClause(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression)));
    }

    /// <summary>
    /// protected virtual void OnPropertyChanged(
    ///     [CallerMemberName] string? propertyName = null)
    /// {
    ///   this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    /// }
    /// </summary>
    /// <returns></returns>
    public MethodDeclarationSyntax CreateOnPropertyChanged(string onPropertyChanged)
    {
        var returnType = GetVoidTypeName();
        var paramName = "propertyName";

        // [CallerMemberName] string propertyName = null
        var parameter = CreateParameterForCallerMemberName(paramName);

        var args = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList<ArgumentSyntax>(new[]
            {
                SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                SyntaxFactory.Argument(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName("PropertyChangedEventArgs"))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("propertyName"))))))
            }));

        var body = SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ConditionalAccessExpression(
                        SyntaxFactory.IdentifierName("PropertyChanged"),
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberBindingExpression(
                                        SyntaxFactory.IdentifierName("Invoke")))
                                .WithArgumentList(args))));

        var declaration = SyntaxFactory.MethodDeclaration(returnType, onPropertyChanged)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.VirtualKeyword)
                ))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SingletonSeparatedList(parameter)))
            .WithBody(body);

        return declaration;
    }

    public SyntaxTrivia CreateNullableEnable()
        => SyntaxFactory.Trivia(
            SyntaxFactory.NullableDirectiveTrivia(
                SyntaxFactory.Token(SyntaxKind.EnableKeyword),
                true));

    public SyntaxTrivia CreateNullableRestore()
        => SyntaxFactory.Trivia(
            SyntaxFactory.NullableDirectiveTrivia(
                SyntaxFactory.Token(SyntaxKind.RestoreKeyword),
                true));

    public TypeSyntax GetTypeName(string typeName)
        => SyntaxFactory.ParseTypeName(typeName);

    public TypeSyntax GetVoidTypeName()
        => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

}


