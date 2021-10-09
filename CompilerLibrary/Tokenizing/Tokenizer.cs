using System.Collections.Generic;
using System.IO;

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
        /// Consumes the next token
        /// </summary>
        /// <returns>The token</returns>
        public Token NextToken()
        {
            throw new System.NotImplementedException();
        }
    }
}
