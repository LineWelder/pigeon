using CompilerLibrary.Parsing.Exceptions;
using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Is used for syntax tree construction
    /// </summary>
    public class Parser
    {
        private Tokenizer tokenizer;

        public Parser(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
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
            if (tokenizer.CurrentToken.Type is not TokenType.Equals)
                throw new UnexpectedTokenException(tokenizer.CurrentToken, "=");

            tokenizer.NextToken();
            SyntaxNode value = ParseExpression();

            if (tokenizer.CurrentToken.Type is not TokenType.Semicolon)
                throw new UnexpectedTokenException(tokenizer.CurrentToken, ";");

            tokenizer.NextToken();
            return new VariableDeclarationNode(
                type.Location, type, name.Value, value
            );
        }
    }
}
