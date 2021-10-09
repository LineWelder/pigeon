using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerLibrary.Tokenizing
{
    /// <summary>
    /// Is used for splitting the source code into tokens
    /// </summary>
    public class Tokenizer
    {
        private readonly Dictionary<char, TokenType> SYMBOLS = new()
        {
            { '=', TokenType.Equals },
            { ';', TokenType.Semicolon }
        };

        /// <summary>
        /// Becomes true when the tokenizer reaches the EOF
        /// </summary>
        public bool ReachedTheEOF { get; private set; }

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

            NextCharacter();
            currentLine = 0;
            currentColumn = 0;
        }

        /// <summary>
        /// Reads the next character in the stream
        /// </summary>
        /// <returns>The read character or 0 if reached the EOF</returns>
        private char NextCharacter()
        {
            int read = stream.Read();
            if (read < 0)
            {
                currentCharacter = '\0';
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
                    currentColumn++;
            }

            return currentCharacter;
        }

        /// <summary>
        /// Skips the white spaces and stops on the first non white space character
        /// </summary>
        /// <returns>The first non white space character</returns>
        private char SkipWhiteSpaces()
        {
            while (currentCharacter != '\0' && char.IsWhiteSpace(currentCharacter))
                NextCharacter();

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
        public Token NextToken()
        {
            SkipWhiteSpaces();
            TokenType tokenType;

            long startPosition = stream.BaseStream.Position;
            Location currentLocation = new(filePath, currentLine, currentColumn, 0);

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

                return new StringToken(
                    currentLocation with { Length = (int)(stream.BaseStream.Position - startPosition) },
                    TokenType.Identifier,
                    identifier.ToString()
                );
            }
            else if (SYMBOLS.TryGetValue(currentCharacter, out tokenType))
            {
                NextCharacter();
                return new Token(
                    currentLocation with { Length = 1 },
                    tokenType
                );
            }
            else if (currentCharacter == '\0')
                return new Token(
                    currentLocation,
                    TokenType.EndOfFile
                );

            return null;
        }
    }
}
