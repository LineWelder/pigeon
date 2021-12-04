using System.Collections.Generic;
using CompilerLibrary.Parsing.Exceptions;
using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing;

/// <summary>
/// Is used for syntax tree construction
/// </summary>
public class Parser
{
    private static readonly Dictionary<TokenType, BinaryNodeOperation> BINARY_OPERATIONS = new()
    {
        { TokenType.Plus, BinaryNodeOperation.Addition },
        { TokenType.Minus, BinaryNodeOperation.Subtraction },
        { TokenType.Star, BinaryNodeOperation.Multiplication },
        { TokenType.Slash, BinaryNodeOperation.Divizion }
    };

    internal static readonly Dictionary<BinaryNodeOperation, int> BINARY_OPERATION_PRIORITIES = new()
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

            _ => throw new UnexpectedTokenException(token, "type")
        };
    }

    /// <summary>
    /// Parses an operand for binary operatios
    /// </summary>
    /// <returns>The parsed node</returns>
    private SyntaxNode ParsePrimaryExpression()
    {
        SyntaxNode result;
        bool negate = false;

        if (tokenizer.CurrentToken.Type is TokenType.Minus)
        {
            tokenizer.NextToken();
            negate = true;
        }

        Token token = tokenizer.CurrentToken;
        tokenizer.NextToken();
        switch (token)
        {
            case StringToken { Type: TokenType.Identifier } identifier:
                result = new IdentifierNode(identifier.Location, identifier.Value);
                break;

            case IntegerToken { Type: TokenType.IntegerLiteral } integer:
                result = new IntegerNode(integer.Location, integer.Value);
                break;

            case { Type: TokenType.LeftParenthesis }:
                result = ParseExpression();
                Consume(TokenType.RightParenthesis, ")");

                break;

            default:
                throw new UnexpectedTokenException(token, "a literal or an identifier");
        }

        if (negate)
        {
            result = new NegationNode(
                tokenizer.CurrentToken.Location,
                result
            );
        }

        while (tokenizer.CurrentToken.Type is TokenType.Colon)
        {
            tokenizer.NextToken();
            result = new TypeCastNode(
                result.Location,
                result, ParseType()
            );
        }

        return result;
    }

    /// <summary>
    /// Parses an expression, but stops when meets an operator with
    /// priority less than depth
    /// </summary>
    /// <param name="depth">Minimal operator priority</param>
    /// <returns>The parsed node</returns>
    private SyntaxNode ParseExpression(int depth = 0)
    {
        if (depth > 2)
            return ParsePrimaryExpression();

        SyntaxNode result = ParseExpression(depth + 1);
        
        while (BINARY_OPERATIONS.TryGetValue(tokenizer.CurrentToken.Type, out BinaryNodeOperation operation)
            && BINARY_OPERATION_PRIORITIES[operation] == depth)
        {
            tokenizer.NextToken();
            SyntaxNode next = ParseExpression(depth + 1);

            result = new BinaryNode(
                result.Location,
                operation,
                Left: result,
                Right: next
            );
        }

        return result;
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
        SyntaxNode result;
        Token firstToken = tokenizer.CurrentToken;

        if (firstToken.Type is TokenType.Return)
        {
            tokenizer.NextToken();
            result = new ReturnNode(
                firstToken.Location,
                ParseExpression()
            );
        }
        else
        {
            SyntaxNode left = ParseExpression();
            Consume(TokenType.Equals, "=");
            result = new AssignmentNode(
                left.Location, left,
                Right: ParseExpression()
            );
        }

        Consume(TokenType.Semicolon, ";");
        return result;
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
    public SyntaxNode ParseDeclaration()
    {
        // First, we assume that it is a function without
        // return type declaration
        SyntaxNode firstNode = ParseType();

        SyntaxNode type = null;
        string name = (firstNode as IdentifierNode)?.Value;
        Location location = firstNode.Location;

        // If it is not, we change the type and name
        if (tokenizer.CurrentToken is StringToken { Type: TokenType.Identifier } trueName)
        {
            type = firstNode;
            name = trueName.Value;

            tokenizer.NextToken();
        }

        Token currentToken = tokenizer.CurrentToken;
        tokenizer.NextToken();
        switch (currentToken.Type)
        {
            // It is a variable declaration
            case TokenType.Equals:
                // It means that we have "smth = ...",
                // so we expect the name instead of =
                if (type is null)
                    throw new UnexpectedTokenException(currentToken, "variable name");

                SyntaxNode value = ParseExpression();
                Consume(TokenType.Semicolon, ";");

                return new VariableDeclarationNode(
                    firstNode.Location, type, name, value
                );

            // It is a function declaration
            case TokenType.LeftParenthesis:
                FunctionArgumentDeclarationNode[] arguments = ParseArgumentListDeclaration();
                SyntaxNode[] body = ParseFunctionBodyDeclaration();

                return new FunctionDeclarationNode(
                    location, type, name,
                    arguments, body
                );

            default:
                throw new UnexpectedTokenException(currentToken, "= or (");
        }
    }

    /// <summary>
    /// Parses all the declarations from a file
    /// </summary>
    /// <returns>The parsed nodes</returns>
    public SyntaxNode[] ParseFile()
    {
        List<SyntaxNode> result = new();
        while (tokenizer.CurrentToken.Type is not TokenType.EndOfFile)
        {
            result.Add(ParseDeclaration());
        }

        return result.ToArray();
    }
}
