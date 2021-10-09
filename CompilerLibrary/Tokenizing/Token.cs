namespace CompilerLibrary.Tokenizing
{
    public enum TokenType
    {
        Identifier,
        IntegerLiteral,
        Equals,
        Semicolon
    }

    /// <summary>
    /// Represents a simple token
    /// </summary>
    public record Token(TokenType Type);

    /// <summary>
    /// Represents a token containing an integer value
    /// </summary>
    public record IntegerToken(TokenType Type, int Value) : Token(Type);

    /// <summary>
    /// Represents a token containing a string value
    /// </summary>
    public record StringToken(TokenType Type, string Value) : Token(Type);
}
