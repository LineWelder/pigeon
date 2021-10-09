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

        private readonly StreamReader stream;
        private char currentCharacter;

        /// <param name="stream">The stream the tokens will be read from</param>
        public Tokenizer(StreamReader stream)
        {
            this.stream = stream;
            ReachedTheEOF = false;

            NextCharacter();
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
                ReachedTheEOF = true;
                read = 0;
            }

            currentCharacter = (char)read;
            return (char)read;
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

                return new StringToken(TokenType.Identifier, identifier.ToString());
            }
            else if (SYMBOLS.TryGetValue(currentCharacter, out tokenType))
            {
                NextCharacter();
                return new Token(tokenType);
            }
            else if (currentCharacter == '\0')
                return new Token(TokenType.EndOfFile);

            return null;
        }
    }
}
