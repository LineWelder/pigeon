using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerLibrary.Tokenizing.Exceptions;

namespace CompilerLibrary.Tokenizing;

/// <summary>
/// Is used for splitting the source code into tokens
/// </summary>
public class Tokenizer
{
    private static readonly Dictionary<char, TokenType> SYMBOLS = new()
    {
        { '=', TokenType.Assign },
        { '+', TokenType.Plus },
        { '-', TokenType.Minus },
        { '*', TokenType.Star },
        { '/', TokenType.Slash },
        { '(', TokenType.LeftParenthesis },
        { ')', TokenType.RightParenthesis },
        { '{', TokenType.LeftCurlyBrace },
        { '}', TokenType.RightCurlyBrace },
        { ',', TokenType.Coma },
        { ';', TokenType.Semicolon },
        { ':', TokenType.Colon }
    };

    private static readonly Dictionary<string, TokenType> KEYWORDS = new()
    {
        { "return", TokenType.ReturnKeyword }
    };

    /// <summary>
    /// Becomes true when the tokenizer reaches the EOF
    /// </summary>
    public bool ReachedTheEOF { get; private set; }

    /// <summary>
    /// The last read token
    /// </summary>
    public Token CurrentToken { get; private set; }

    private readonly string filePath;
    private readonly StreamReader stream;

    private char currentCharacter;
    private int currentLine;
    private int currentColumn;

    /// <param name="stream">The stream the tokens will be read from</param>
    public Tokenizer(string filePath, StreamReader stream)
    {
        this.filePath = filePath;
        this.stream = stream;
        ReachedTheEOF = false;
        CurrentToken = null!;

        NextCharacter();
        currentLine = 0;
        currentColumn = 0;
    }

    /// <summary>
    /// Reads the next character in the stream
    /// </summary>
    /// <returns>The read character or 0 if reached the EOF</returns>
    private void NextCharacter()
    {
        int read = stream.Read();
        if (read < 0)
        {
            currentCharacter = '\0';
            if (currentColumn == -1)
            {
                currentColumn = 0;
            }

            ReachedTheEOF = true;
        }
        else
        {
            currentCharacter = (char)read;

            if (currentCharacter == '\n')
            {
                currentColumn = -1;
                currentLine++;
            }
            else
            {
                currentColumn++;
            }
        }
    }

    /// <summary>
    /// Skips the white spaces and stops on the first non white space character
    /// </summary>
    /// <returns>The first non white space character</returns>
    private char SkipWhiteSpaces()
    {
        while (currentCharacter != '\0' && char.IsWhiteSpace(currentCharacter))
        {
            NextCharacter();
        }

        return currentCharacter;
    }

    /// <summary>
    /// Checkes if an identifier can start with the given character
    /// </summary>
    private static bool IsValidIdentifierStarter(char ch)
        => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';

    /// <summary>
    /// Consumes the next token
    /// </summary>
    /// <returns>The token</returns>
    public void NextToken()
    {
        SkipWhiteSpaces();
        Location currentLocation = new(filePath, currentLine, currentColumn);

        // Identifier
        if (IsValidIdentifierStarter(currentCharacter))
        {
            StringBuilder identifier = new();
            identifier.Append(currentCharacter);

            // Digits can be used in identifiers, but not as the first character
            NextCharacter();
            while (IsValidIdentifierStarter(currentCharacter) || char.IsDigit(currentCharacter))
            {
                identifier.Append(currentCharacter);
                NextCharacter();
            }

            string value = identifier.ToString();
            if (KEYWORDS.TryGetValue(value, out TokenType tokenType))
            {
                CurrentToken = new Token(
                    currentLocation,
                    tokenType
                );
            }
            else
            {
                CurrentToken = new StringToken(
                    currentLocation,
                    TokenType.Identifier,
                    value
                );
            }
        }

        // Integer literal
        else if (char.IsDigit(currentCharacter))
        {
            long value = currentCharacter - '0';
            int length = 1;

            NextCharacter();
            while (char.IsDigit(currentCharacter))
            {
                value = 10 * value + currentCharacter - '0';
                length++;
                NextCharacter();
            }

            CurrentToken = new IntegerToken(
                currentLocation,
                TokenType.IntegerLiteral,
                value
            );
        }

        // Symbol
        else if (SYMBOLS.TryGetValue(currentCharacter, out TokenType tokenType))
        {
            NextCharacter();
            CurrentToken = new Token(
                currentLocation,
                tokenType
            );
        }

        // End of file
        else if (currentCharacter == '\0')
        {
            CurrentToken = new Token(
                currentLocation,
                TokenType.EndOfFile
            );
        }

        else
        {
            throw new UnexpectedCharacterException(currentLocation, currentCharacter);
        }
    }
}