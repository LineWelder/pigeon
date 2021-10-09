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

        private readonly StreamReader stream;

        /// <param name="stream">The stream the tokens will be read from</param>
        public Tokenizer(StreamReader stream)
        {
            this.stream = stream;
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
            char nextCharacter = (char)stream.Read();

            if (IsValidIdentifierStarter(nextCharacter))
            {
                StringBuilder identifier = new();
                identifier.Append(nextCharacter);

                // Digits can be used in identifiers, but not as the first character
                nextCharacter = (char)stream.Read();
                while (IsValidIdentifierStarter(nextCharacter) || char.IsDigit(nextCharacter))
                {
                    identifier.Append(nextCharacter);
                    nextCharacter = (char)stream.Read();
                }

                return new StringToken(TokenType.Identifier, identifier.ToString());
            }

            return null;
        }
    }
}
