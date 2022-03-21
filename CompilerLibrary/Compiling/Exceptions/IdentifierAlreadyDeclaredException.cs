using CompilerLibrary.Parsing;
using System;

namespace CompilerLibrary.Compiling.Exceptions;

public class IdentifierAlreadyDeclaredException : CompilerException
{
    public SyntaxNode Redeclaration { get; init; }
    public Location PreviousDeclaration { get; init; }

    public string Identifier => GetIdentifier(Redeclaration);

    public IdentifierAlreadyDeclaredException(
        SyntaxNode redeclaration, Location previousDeclaration)
        : base(
            redeclaration.Location,
            $"Identifier {GetIdentifier(redeclaration)} already declared in {previousDeclaration}"
        )
    {
        Redeclaration = redeclaration;
        PreviousDeclaration = previousDeclaration;
    }

    private static string GetIdentifier(SyntaxNode node)
        => node switch
        {
            VariableDeclarationNode variable => variable.Identifier,
            FunctionDeclarationNode function => function.Identifier,
            FunctionArgumentDeclarationNode argument => argument.Identifier,
            _ => throw new Exception("Unexpected type")
        };
}