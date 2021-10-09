using CompilerLibrary.Parsing.Exceptions;
using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Is used for syntax tree construction
    /// </summary>
    public class Parser
    {
        private readonly Tokenizer tokenizer;

        public Parser(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
            tokenizer.NextToken();
        }

        /// <summary>
        /// Checks if the current token is of the right type and advances
        /// </summary>
        /// <param name="type">The expected token type</param>
        /// <param name="expectation">What was expected</param>
        private void Consume(TokenType type, string expectation)
        {
            if (tokenizer.CurrentToken.Type != type)
                throw new UnexpectedTokenException(tokenizer.CurrentToken, expectation);

            tokenizer.NextToken();
        }

        /// <summary>
        /// Parses a type
        /// </summary>
        /// <returns>The parsed node</returns>
        private SyntaxNode ParseType()
        {
            Token token = tokenizer.CurrentToken;
            tokenizer.NextToken();

            return token switch
            {
                StringToken { Type: TokenType.Identifier } identifier =>
                    new IdentifierNode(identifier.Location, identifier.Value),

                _ => throw new UnexpectedTokenException(tokenizer.CurrentToken)
            };
        }

        /// <summary>
        /// Parses an expression
        /// </summary>
        /// <returns>The parsed node</returns>
        private SyntaxNode ParseExpression()
        {
            Token token = tokenizer.CurrentToken;
            tokenizer.NextToken();

            return token switch
            {
                StringToken { Type: TokenType.Identifier } identifier =>
                    new IdentifierNode(identifier.Location, identifier.Value),

                IntegerToken { Type: TokenType.IntegerLiteral } integer =>
                    new IntegerNode(integer.Location, integer.Value),

                _ => throw new UnexpectedTokenException(token, "expression")
            };
        }

        /// <summary>
        /// Parses a single declaration statement
        /// </summary>
        /// <returns>The parsed node</returns>
        public SyntaxNode Parse()
        {
            SyntaxNode type = ParseType();

            if (tokenizer.CurrentToken is not StringToken { Type: TokenType.Identifier } name)
                throw new UnexpectedTokenException(tokenizer.CurrentToken, "variable name");

            tokenizer.NextToken();
            Consume(TokenType.Equals, "=");

            SyntaxNode value = ParseExpression();
            Consume(TokenType.Semicolon, ";");

            return new VariableDeclarationNode(
                type.Location, type, name.Value, value
            );
        }
    }
}
