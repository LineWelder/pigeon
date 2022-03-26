using System.Collections.Generic;
using CompilerLibrary.Parsing.Exceptions;
using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing;

/// <summary>
/// Is used for syntax tree construction
/// </summary>
public class Parser
{
    internal static readonly Dictionary<TokenType, BinaryNodeOperation> BINARY_OPERATIONS = new()
    {
        { TokenType.Plus, BinaryNodeOperation.Addition },
        { TokenType.Minus, BinaryNodeOperation.Subtraction },
        { TokenType.Star, BinaryNodeOperation.Multiplication },
        { TokenType.Slash, BinaryNodeOperation.Divizion },
        { TokenType.Equals, BinaryNodeOperation.EqualityCheck },
        { TokenType.NotEquals, BinaryNodeOperation.InequalityCheck },
        { TokenType.Less, BinaryNodeOperation.LessThanCheck },
        { TokenType.Greater, BinaryNodeOperation.GreaterThanCheck },
        { TokenType.LessEquals, BinaryNodeOperation.LessEqualCheck },
        { TokenType.GreaterEquals, BinaryNodeOperation.GreaterEqualCheck },
    };

    internal static readonly Dictionary<BinaryNodeOperation, int> BINARY_OPERATION_PRIORITIES = new()
    {
        { BinaryNodeOperation.EqualityCheck, 0 },
        { BinaryNodeOperation.InequalityCheck, 0 },
        { BinaryNodeOperation.LessThanCheck, 0 },
        { BinaryNodeOperation.GreaterThanCheck, 0 },
        { BinaryNodeOperation.LessEqualCheck, 0 },
        { BinaryNodeOperation.GreaterEqualCheck, 0 },
        { BinaryNodeOperation.Addition, 1 },
        { BinaryNodeOperation.Subtraction, 1 },
        { BinaryNodeOperation.Multiplication, 2 },
        { BinaryNodeOperation.Divizion, 2 }
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
        {
            throw new UnexpectedTokenException(tokenizer.CurrentToken, expectation);
        }

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

        if (token is not StringToken { Type: TokenType.Identifier } identifier)
        {
            throw new UnexpectedTokenException(token, "type");
        }

        return new IdentifierNode(identifier.Location, identifier.Value);
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

    lookForPostfix:
        switch (tokenizer.CurrentToken.Type)
        {
            case TokenType.Colon:
                tokenizer.NextToken();
                result = new TypeCastNode(
                    result.Location,
                    result, ParseType()
                );

                goto lookForPostfix;

            case TokenType.LeftParenthesis:
                tokenizer.NextToken();
                result = new FunctionCallNode(
                    result.Location,
                    result, ParseFunctionCallArguments()
                );

                goto lookForPostfix;
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
        {
            return ParsePrimaryExpression();
        }

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
    /// Parses arguments for a function call skipping the left parenthesis
    /// </summary>
    /// <returns>The parsed nodes</returns>
    private SyntaxNode[] ParseFunctionCallArguments()
    {
        List<SyntaxNode> argumentList = new();

        if (tokenizer.CurrentToken.Type is TokenType.RightParenthesis)
        {
            goto endOfArgumentList;
        }

        while (true)
        {
            argumentList.Add(ParseExpression());

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
    /// Parses arguments for function declaration skipping the left parenthesis
    /// </summary>
    /// <returns>The parsed nodes</returns>
    private FunctionArgumentDeclarationNode[] ParseFunctionArgumentsDeclaration()
    {
        List<FunctionArgumentDeclarationNode> argumentList = new();

        if (tokenizer.CurrentToken.Type is TokenType.RightParenthesis)
        {
            goto endOfArgumentList;
        }

        while (true)
        {
            SyntaxNode argumentType = ParseType();
            if (tokenizer.CurrentToken is not StringToken { Type: TokenType.Identifier } name)
            {
                throw new UnexpectedTokenException(tokenizer.CurrentToken, "argument name");
            }

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
    /// <returns>The parsed node</returns>
    private SyntaxNode ParseStatement()
    {
        SyntaxNode result;
        Token firstToken = tokenizer.CurrentToken;

        switch (firstToken.Type)
        {
            case TokenType.LeftCurlyBrace:
                tokenizer.NextToken();

                List<SyntaxNode> innerStatements = new();
                while (tokenizer.CurrentToken.Type is not TokenType.RightCurlyBrace)
                {
                    innerStatements.Add(ParseStatement());
                }
                tokenizer.NextToken();

                result = new CompoundStatementNode(
                    firstToken.Location,
                    innerStatements.ToArray()
                );
                break;

            case TokenType.IfKeyword:
                tokenizer.NextToken();
                Consume(TokenType.LeftParenthesis, "(");
                SyntaxNode ifCondition = ParseExpression();
                Consume(TokenType.RightParenthesis, ")");
                SyntaxNode thenBranch = ParseStatement();

                SyntaxNode? elseBranch = null;
                if (tokenizer.CurrentToken.Type is TokenType.ElseKeyword)
                {
                    tokenizer.NextToken();
                    elseBranch = ParseStatement();
                }

                result = new BranchingNode(
                    firstToken.Location,
                    ifCondition, thenBranch, elseBranch
                );
                break;

            case TokenType.WhileKeyword:
                tokenizer.NextToken();
                Consume(TokenType.LeftParenthesis, "(");
                SyntaxNode whileCondition = ParseExpression();
                Consume(TokenType.RightParenthesis, ")");
                SyntaxNode whileBody = ParseStatement();

                result = new WhileLoopNode(
                    firstToken.Location,
                    whileCondition, whileBody
                );
                break;

            case TokenType.ReturnKeyword:
                tokenizer.NextToken();
                if (tokenizer.CurrentToken.Type is TokenType.Semicolon)
                {
                    tokenizer.NextToken();
                    return new ReturnNode(firstToken.Location, null);
                }

                result = new ReturnNode(
                    firstToken.Location,
                    ParseExpression()
                );

                Consume(TokenType.Semicolon, ";");
                break;

            default:
                SyntaxNode left = ParseExpression();

                if (left is FunctionCallNode)
                {
                    if (tokenizer.CurrentToken.Type is TokenType.Semicolon)
                    {
                        tokenizer.NextToken();
                        return left;
                    }

                    Consume(TokenType.Assign, "= or ;");
                }
                else
                {
                    Consume(TokenType.Assign, "=");
                }

                result = new AssignmentNode(
                    left.Location, left,
                    Right: ParseExpression()
                );

                Consume(TokenType.Semicolon, ";");
                break;
        }

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
        {
            statementList.Add(ParseStatement());
        }

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

        SyntaxNode? type = null;
        string name;
        Location location = firstNode.Location;

        // If the next token is an identifier then it is the real name
        if (tokenizer.CurrentToken is StringToken { Type: TokenType.Identifier } realName)
        {
            type = firstNode;
            name = realName.Value;
            tokenizer.NextToken();
        }
        // If the first token was the name
        else if (firstNode is IdentifierNode identifierNode)
        {
            name = identifierNode.Value;
        }
        // Else it is illegal syntax and we expect the name go after the type
        else
        {
            throw new UnexpectedTokenException(tokenizer.CurrentToken, "name");
        }

        Token currentToken = tokenizer.CurrentToken;
        tokenizer.NextToken();
        switch (currentToken.Type)
        {
            // It is a variable declaration
            case TokenType.Assign:
                // It means that we have "smth = ...",
                // so we expect the name instead of =
                if (type is null)
                {
                    throw new UnexpectedTokenException(currentToken, "variable name");
                }

                SyntaxNode value = ParseExpression();
                Consume(TokenType.Semicolon, ";");

                return new VariableDeclarationNode(
                    firstNode.Location, type, name, value
                );

            // It is a function declaration
            case TokenType.LeftParenthesis:
                FunctionArgumentDeclarationNode[] arguments = ParseFunctionArgumentsDeclaration();
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
