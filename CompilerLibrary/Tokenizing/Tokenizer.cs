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

        /// <param name="stream">The stream the tokens will be read from</param>
        public Tokenizer(StreamReader stream)
        {
            this.stream = stream;
            ReachedTheEOF = false;
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
                return '\0';
            }

            return (char)read;
        }

        /// <summary>
        /// Skips the white spaces and stops on the first non white space character
        /// </summary>
        /// <returns>The first non white space character</returns>
        private char SkipWhiteSpaces()
        {
            char nextCharacter;

            do nextCharacter = NextCharacter();
            while (nextCharacter != '\0' && char.IsWhiteSpace(nextCharacter));

            return nextCharacter;
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
            char nextCharacter = SkipWhiteSpaces();

            if (IsValidIdentifierStarter(nextCharacter))
            {
                StringBuilder identifier = new();
                identifier.Append(nextCharacter);

                // Digits can be used in identifiers, but not as the first character
                nextCharacter = NextCharacter();
                while (IsValidIdentifierStarter(nextCharacter) || char.IsDigit(nextCharacter))
                {
                    identifier.Append(nextCharacter);
                    nextCharacter = NextCharacter();
                }

                return new StringToken(TokenType.Identifier, identifier.ToString());
            }
            else if (nextCharacter == '\0')
                return new Token(TokenType.EndOfFile);

            return null;
        }
    }
}
