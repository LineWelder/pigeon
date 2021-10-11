using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing.Exceptions
{
    public class UnexpectedTokenException : CompilerException
    {
        public Token Token { get; init; }

        public UnexpectedTokenException(Token token, string expectation)
            : base(token.Location, $"Unexpected token type {token.Type}, {expectation} expected")
        {
            Token = token;
        }
    }
}
