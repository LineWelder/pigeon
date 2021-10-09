namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Represents a simple syntax node
    /// </summary>
    public record SyntaxNode(Location Location);

    /// <summary>
    /// Represents a variable declaration
    /// <VariableType> <Identifier> = <Value>;
    /// </summary>
    public record VariableDeclarationNode(
        Location Location,
        SyntaxNode VariableType, string Identifier, SyntaxNode Value
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
}
