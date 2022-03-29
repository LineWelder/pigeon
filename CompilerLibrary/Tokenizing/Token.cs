namespace CompilerLibrary.Tokenizing;

public enum TokenType
{
    EndOfFile,
    Identifier,
    IntegerLiteral,
    Assign,
    Plus, Minus, Star, Slash,
    Equals, NotEquals, Less, Greater, LessEquals, GreaterEquals,
    And, Or, Not,
    LeftParenthesis, RightParenthesis,
    LeftCurlyBrace, RightCurlyBrace,
    Coma, Semicolon,
    Colon,
    IfKeyword, ElseKeyword, WhileKeyword,
    ReturnKeyword
}

/// <summary>
/// Represents a simple token
/// </summary>
public record Token(Location Location, TokenType Type);

/// <summary>
/// Represents a token containing an integer value
/// </summary>
public record IntegerToken(Location Location, TokenType Type, long Value)
    : Token(Location, Type);

/// <summary>
/// Represents a token containing a string value
/// </summary>
public record StringToken(Location Location, TokenType Type, string Value)
    : Token(Location, Type);