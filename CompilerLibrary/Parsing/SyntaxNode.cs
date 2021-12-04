namespace CompilerLibrary.Parsing;

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
    SyntaxNode? ReturnType, string Identifier,
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

public enum BinaryNodeOperation
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
    Location Location, BinaryNodeOperation Operation,
    SyntaxNode Left, SyntaxNode Right
) : SyntaxNode(Location);

/// <summary>
/// Represents a negation expression
/// </summary>
public record NegationNode(
    Location Location, SyntaxNode InnerExpression
) : SyntaxNode(Location);

/// <summary>
/// Represents an explicit type cast expression
/// </summary>
public record TypeCastNode(
    Location Location,
    SyntaxNode Value, SyntaxNode Type
) : SyntaxNode(Location);

/// <summary>
/// Represents an assignment statement
/// </summary>
public record AssignmentNode(
    Location Location,
    SyntaxNode Left, SyntaxNode Right
) : SyntaxNode(Location);

/// <summary>
/// Represents a return statement
/// </summary>
public record ReturnNode(
    Location Location,
    SyntaxNode InnerExpression
) : SyntaxNode(Location);