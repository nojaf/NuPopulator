namespace NuPopulator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class RoslynCodeGen
{
    private static NamespaceDeclarationSyntax CreateNamespaceDeclaration(
        string namespaceName,
        params ClassDeclarationSyntax[] types
    )
    {
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
            .NormalizeWhitespace();

        return types.Aggregate(namespaceDeclaration, (current, type) => current.AddMembers(type));
    }

    private static SyntaxToken[] CreateModifiers(bool isStatic)
    {
        return isStatic
            ? new[]
            {
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
            }
            : new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };
    }

    private static ClassDeclarationSyntax CreateClassDeclaration(
        bool isStatic,
        string className,
        params MemberDeclarationSyntax[] members
    )
    {
        var classDeclaration = SyntaxFactory
            .ClassDeclaration(className)
            .AddModifiers(CreateModifiers(isStatic));
        return classDeclaration.AddMembers(members);
    }

    private static PropertyDeclarationSyntax CreatePropertyDeclaration(
        string propertyName,
        TypeSyntax propertyType,
        ExpressionSyntax initialValue
    )
    {
        return SyntaxFactory
            .PropertyDeclaration(propertyType, propertyName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            )
            .WithInitializer(SyntaxFactory.EqualsValueClause(initialValue))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static MethodDeclarationSyntax CreateMethodDeclaration(
        bool isStatic,
        TypeSyntax returnType,
        string methodName,
        ParameterSyntax[] parameters,
        ExpressionSyntax returnExpression
    )
    {
        return SyntaxFactory
            .MethodDeclaration(returnType, methodName)
            .AddModifiers(CreateModifiers(isStatic))
            .AddParameterListParameters(parameters)
            .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(returnExpression)));
    }

    private static ParameterSyntax CreateParameter(TypeSyntax type, string name)
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(type);
    }

    private static ExpressionSyntax CreateIntegerExpression(int value)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value)
        );
    }

    private static TypeSyntax IntegerType =>
        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));

    private static TypeSyntax GetPrefixedType(string namespaceName, string typeName)
    {
        return SyntaxFactory.QualifiedName(
            SyntaxFactory.IdentifierName(namespaceName),
            SyntaxFactory.IdentifierName(typeName)
        );
    }

    private static ExpressionSyntax CreateDefaultExpression(TypeSyntax type)
    {
        return SyntaxFactory.DefaultExpression(type);
    }

    private static string MkString(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        var syntaxTree = SyntaxFactory.SyntaxTree(namespaceDeclaration.NormalizeWhitespace());
        return syntaxTree.ToString();
    }

    public static string MkType(IEnumerable<string> typeNames, string namespaceName)
    {
        var types = typeNames
            .Select(name =>
            {
                var propertyDeclaration = CreatePropertyDeclaration(
                    "Value",
                    IntegerType,
                    CreateIntegerExpression(3)
                );
                var methodDeclaration = CreateMethodDeclaration(
                    false,
                    IntegerType,
                    "Fn",
                    [],
                    CreateIntegerExpression(4)
                );
                return CreateClassDeclaration(false, name, propertyDeclaration, methodDeclaration);
            })
            .ToArray();
        return MkString(CreateNamespaceDeclaration(namespaceName, types));
    }

    public static string MkConsume(
        IEnumerable<string> typeNames,
        string namespaceName,
        IEnumerable<string> referencedProjects
    )
    {
        var methods = typeNames
            .Select(typeName =>
            {
                var parameters = referencedProjects
                    .Select(
                        (rp, idx) =>
                        {
                            var t = GetPrefixedType(rp, typeName);
                            return CreateParameter(t, $"p{idx}");
                        }
                    )
                    .ToArray();

                var bodyExpr = referencedProjects
                    .SelectMany(
                        (rp, idx) =>
                        {
                            var identifier = SyntaxFactory.IdentifierName($"p{idx}");
                            return new ExpressionSyntax[]
                            {
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        identifier,
                                        SyntaxFactory.IdentifierName("Fn")
                                    )
                                ),
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    identifier,
                                    SyntaxFactory.IdentifierName("Value")
                                ),
                            };
                        }
                    )
                    .Aggregate(
                        CreateIntegerExpression(0),
                        (acc, expr) =>
                            SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, acc, expr)
                    );

                var t = SyntaxFactory.IdentifierName(typeName);
                var method = CreateMethodDeclaration(
                    true,
                    IntegerType,
                    $"Function{typeName}",
                    parameters,
                    bodyExpr
                );
                return method;
            })
            .ToArray();

        var classDeclaration = CreateClassDeclaration(true, "Consumer", methods);
        var namespaceDeclaration = CreateNamespaceDeclaration(namespaceName, classDeclaration);
        return MkString(namespaceDeclaration);
    }
}
