﻿namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Represents a simple syntax node
    /// </summary>
    public record SyntaxNode(Location Location);

    /// <summary>
    /// Represents a variable declaration
    /// </summary>
    public record VariableDeclarationNode(
        Location Location,
        SyntaxNode Type, string Identifier, SyntaxNode Value
    ) : SyntaxNode(Location);

    /// <summary>
    /// Represents a function argument declaration
    /// </summary>
    public record FunctionArgumentDeclarationNode(
        Location Location,
        SyntaxNode Type, string Identifier
    ) : SyntaxNode(Location);

    /// <summary>
    /// Represents a function declaration
    /// </summary>
    public record FunctionDeclarationNode(
        Location Location,
        SyntaxNode ReturnType, string Identifier,
        FunctionArgumentDeclarationNode[] ArgumentList,
        SyntaxNode[] Body
    ) : SyntaxNode(Location);

    /// <summary>
    /// Represents an identifier
    /// </summary>
    public record IdentifierNode(
        Location Location, string Value
    ) : SyntaxNode(Location);

    /// <summary>
    /// Represents an integer literal
    /// </summary>
    public record IntegerNode(
        Location Location, long Value
    ) : SyntaxNode(Location);

    public enum BinaryNodeType
    {
        Addition,
        Subtraction,
        Multiplication,
        Divizion
    }

    /// <summary>
    /// Represents a binary expression
    /// </summary>
    public record BinaryNode(
        Location Location, BinaryNodeType Type,
        SyntaxNode Left, SyntaxNode Right
    ) : SyntaxNode(Location);
}