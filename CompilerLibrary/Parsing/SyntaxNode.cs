namespace CompilerLibrary.Parsing
{
    public enum SyntaxNodeType
    {
        VariableDeclaration,
        Identifier
    }

    /// <summary>
    /// Represents a simple syntax node
    /// </summary>
    public record SyntaxNode(Location Location, SyntaxNodeType Type);

    /// <summary>
    /// Represents a variable declaration
    /// <VariableType> <Identifier> = <Value>;
    /// </summary>
    public record VariableDeclarationNode(
        Location Location, SyntaxNodeType Type,
        SyntaxNode VariableType, string Identifier, SyntaxNode Value
    ) : SyntaxNode(Location, Type);
}
