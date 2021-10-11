using System.Collections.Generic;
using CompilerLibrary.Parsing.Exceptions;
using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Is used for syntax tree construction
    /// </summary>
    public class Parser
    {
        private readonly Dictionary<TokenType, BinaryNodeOperation> BINARY_OPERATIONS = new()
        {
            { TokenType.Plus, BinaryNodeOperation.Addition },
            { TokenType.Minus, BinaryNodeOperation.Subtraction },
            { TokenType.Star, BinaryNodeOperation.Multiplication },
            { TokenType.Slash, BinaryNodeOperation.Divizion }
        };

        private readonly Dictionary<BinaryNodeOperation, int> BINARY_OPERATION_PRIORITIES = new()
        {
            { BinaryNodeOperation.Addition, 0 },
            { BinaryNodeOperation.Subtraction, 0 },
            { BinaryNodeOperation.Multiplication, 1 },
            { BinaryNodeOperation.Divizion, 1 }
        };

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

                _ => throw new UnexpectedTokenException(tokenizer.CurrentToken, "type")
            };
        }

        /// <summary>
        /// Parses an expression
        /// </summary>
        /// <returns>The parsed node</returns>
        private SyntaxNode ParseExpression()
        {
            SyntaxNode primaryExpression;

            Token firstToken = tokenizer.CurrentToken;
            tokenizer.NextToken();

            switch (firstToken)
            {
                case StringToken { Type: TokenType.Identifier } identifier:
                    primaryExpression = new IdentifierNode(identifier.Location, identifier.Value);
                    break;

                case IntegerToken { Type: TokenType.IntegerLiteral } integer:
                    primaryExpression = new IntegerNode(integer.Location, integer.Value);
                    break;

                case { Type: TokenType.LeftParenthesis }:
                    primaryExpression = ParseExpression();
                    Consume(TokenType.RightParenthesis, ")");
                    break;

                default:
                    throw new UnexpectedTokenException(firstToken, "expression");
            }

            if (BINARY_OPERATIONS.TryGetValue(tokenizer.CurrentToken.Type, out var operationType))
            {
                tokenizer.NextToken();
                bool rightExpressionIsInParentheses = tokenizer.CurrentToken.Type is TokenType.LeftParenthesis;

                SyntaxNode rightExpression = ParseExpression();

                // By default the expression will be returned right-to-left, but
                // if the right operation is not more prior than the current
                // then we need to make the right operation the parent one
                // op(a, op(b, c)) -> op(op(a, b), c)
                if (!rightExpressionIsInParentheses
                    && rightExpression is BinaryNode rightExpressionBinary
                    && BINARY_OPERATION_PRIORITIES[rightExpressionBinary.Operation]
                           <= BINARY_OPERATION_PRIORITIES[operationType])
                {
                    return new BinaryNode(
                        primaryExpression.Location,
                        rightExpressionBinary.Operation,
                        Left: new BinaryNode(
                            primaryExpression.Location,
                            operationType,
                            primaryExpression,
                            rightExpressionBinary.Left
                        ),
                        Right: rightExpressionBinary.Right
                    );
                }

                return new BinaryNode(
                    primaryExpression.Location,
                    operationType,
                    Left: primaryExpression,
                    Right: rightExpression
                );
            }

            // If there is no operator
            return primaryExpression;
        }

        /// <summary>
        /// Parses arguments for function declaration skipping the left parenthesis
        /// </summary>
        /// <returns>The parsed nodes</returns>
        private FunctionArgumentDeclarationNode[] ParseArgumentListDeclaration()
        {
            List<FunctionArgumentDeclarationNode> argumentList = new();

            if (tokenizer.CurrentToken.Type is TokenType.RightParenthesis)
                goto endOfArgumentList;

            while (true)
            {
                SyntaxNode argumentType = ParseType();
                if (tokenizer.CurrentToken is not StringToken { Type: TokenType.Identifier } name)
                    throw new UnexpectedTokenException(tokenizer.CurrentToken, "argument name");

                argumentList.Add(new FunctionArgumentDeclarationNode(
                    argumentType.Location,
                    argumentType,
                    name.Value
                ));

                tokenizer.NextToken();

                switch (tokenizer.CurrentToken.Type)
                {
                    case TokenType.RightParenthesis:
                        goto endOfArgumentList;

                    case not TokenType.Coma:
                        throw new UnexpectedTokenException(tokenizer.CurrentToken, ", or )");
                }

                tokenizer.NextToken();
            }

        endOfArgumentList:
            tokenizer.NextToken();
            return argumentList.ToArray();
        }

        /// <summary>
        /// Parses a single statement
        /// </summary>
        /// <remarks>Supports only assignment statements now</remarks>
        /// <returns>The parsed node</returns>
        private SyntaxNode ParseStatement()
        {
            SyntaxNode left = ParseExpression();

            Consume(TokenType.Equals, "=");

            SyntaxNode right = ParseExpression();

            Consume(TokenType.Semicolon, ";");

            return new AssignmentNode(left.Location, left, right);
        }

        /// <summary>
        /// Parses function body declaration
        /// </summary>
        /// <returns>The parsed nodes</returns>
        private SyntaxNode[] ParseFunctionBodyDeclaration()
        {
            Consume(TokenType.LeftCurlyBrace, "{");
            List<SyntaxNode> statementList = new();

            while (tokenizer.CurrentToken.Type is not TokenType.RightCurlyBrace)
                statementList.Add(ParseStatement());

            tokenizer.NextToken();
            return statementList.ToArray();
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
            Token currentToken = tokenizer.CurrentToken;

            tokenizer.NextToken();
            switch (currentToken.Type)
            {
                // It is a variable declaration
                case TokenType.Equals:
                    SyntaxNode value = ParseExpression();
                    Consume(TokenType.Semicolon, ";");

                    return new VariableDeclarationNode(
                        type.Location, type, name.Value, value
                    );

                // It is a function declaration
                case TokenType.LeftParenthesis:
                    FunctionArgumentDeclarationNode[] arguments = ParseArgumentListDeclaration();
                    SyntaxNode[] body = ParseFunctionBodyDeclaration();

                    return new FunctionDeclarationNode(
                        type.Location, type, name.Value,
                        arguments, body
                    );

                default:
                    throw new UnexpectedTokenException(currentToken, "= or (");
            }
        }
    }
}
