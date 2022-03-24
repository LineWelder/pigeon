using CompilerLibrary.Tokenizing;
using System;
using System.Linq;

namespace CompilerLibrary.Parsing;

public static class Debug
{
    /// <summary>
    /// Finds the given value in the given dictionary
    /// </summary>
    /// <returns>The key of the found value</returns>
    private static string GetOperationOperator(BinaryNodeOperation operation)
    {
        TokenType operationToken = Parser.BINARY_OPERATIONS
            .Where(pair => pair.Value == operation)
            .FirstOrDefault()
            .Key;
        if (operationToken == TokenType.EndOfFile)
        {
            throw new ArgumentException(
                "The corresponding token not found",
                nameof(operation)
            );
        }

        char symbol = Tokenizer.SYMBOLS
            .Where(pair => pair.Value == operationToken)
            .FirstOrDefault()
            .Key;
        if (symbol != '\0')
        {
            return symbol.ToString();
        }

        string? doubleSymbol = Tokenizer.DOUBLE_SYMBOLS
            .Where(pair => pair.Value == operationToken)
            .FirstOrDefault()
            .Key;
        if (doubleSymbol is not null)
        {
            return doubleSymbol.ToString();
        }

        throw new ArgumentException(
            "The corresponding operator not found",
            nameof(operation)
        );
    }

    /// <summary>
    /// Prints the given syntax node into console and
    /// puts parentheses around it if required
    /// </summary>
    /// <param name="putParentheses">Specifies whether to put the parentheses or not</param>
    private static void PrintInParenthesesIfNeeded(SyntaxNode node, bool putParentheses)
    {
        if (putParentheses) Console.Write('(');
        PrintSyntaxNode(node);
        if (putParentheses) Console.Write(')');
    }

    /// <summary>
    /// Prints the given syntax node into console
    /// with <paramref name="offset"/> spaces before each line
    /// </summary>
    /// <param name="node">The node to print</param>
    /// <param name="offset">The number of spaces to print before each line</param>
    public static void PrintSyntaxNode(SyntaxNode node, int offset = 0)
    {
        void MakeOffset()
        {
            for (int i = 0; i < offset; i++)
            {
                Console.Write(' ');
            }
        }

        MakeOffset();
        switch (node)
        {
            case FunctionDeclarationNode functionDeclaration:
                if (functionDeclaration.ReturnType is not null)
                {
                    PrintSyntaxNode(functionDeclaration.ReturnType);
                    Console.Write(' ');
                }
                Console.Write($"{functionDeclaration.Identifier}(");

                for (int i = 0; i < functionDeclaration.Arguments.Length; i++)
                {
                    PrintSyntaxNode(functionDeclaration.Arguments[0].Type);
                    Console.Write($" {functionDeclaration.Arguments[0].Identifier}");

                    if (i < functionDeclaration.Arguments.Length - 1)
                    {
                        Console.Write(", ");
                    }
                }
                Console.WriteLine(')');

                MakeOffset();
                Console.WriteLine('{');
                foreach (SyntaxNode statement in functionDeclaration.Body)
                {
                    PrintSyntaxNode(statement, offset + 4);
                    Console.WriteLine();
                }
                Console.Write('}');

                break;

            case VariableDeclarationNode variableDeclaration:
                PrintSyntaxNode(variableDeclaration.Type);
                Console.Write($" {variableDeclaration.Identifier} = ");
                PrintSyntaxNode(variableDeclaration.Value);

                break;

            case IdentifierNode identifier:
                Console.Write(identifier.Value);
                break;

            case IntegerNode integer:
                Console.Write(integer.Value);
                break;

            case TypeCastNode typeCast:
                PrintInParenthesesIfNeeded(typeCast.Value, typeCast.Value is BinaryNode);
                Console.Write(':');
                PrintSyntaxNode(typeCast.Type);

                break;

            case NegationNode negation:
                Console.Write('-');
                PrintInParenthesesIfNeeded(
                    negation.InnerExpression,
                    negation.InnerExpression is not (IntegerNode or IdentifierNode)
                );

                break;

            case BinaryNode binary:
                bool leftParentheses =
                    binary.Left is BinaryNode { Operation: BinaryNodeOperation leftOperation }
                 && Parser.BINARY_OPERATION_PRIORITIES[leftOperation]
                        < Parser.BINARY_OPERATION_PRIORITIES[binary.Operation];

                bool rightParentheses =
                    binary.Right is BinaryNode { Operation: BinaryNodeOperation rightOperation }
                 && Parser.BINARY_OPERATION_PRIORITIES[rightOperation]
                        <= Parser.BINARY_OPERATION_PRIORITIES[binary.Operation];

                PrintInParenthesesIfNeeded(binary.Left, leftParentheses);
                Console.Write($" {GetOperationOperator(binary.Operation)} ");
                PrintInParenthesesIfNeeded(binary.Right, rightParentheses);

                break;

            case AssignmentNode assignment:
                PrintSyntaxNode(assignment.Left);
                Console.Write(" = ");
                PrintSyntaxNode(assignment.Right);
                Console.Write(';');

                break;

            case ReturnNode @return:
                Console.Write("return ");
                if (@return.InnerExpression is not null)
                {
                    PrintSyntaxNode(@return.InnerExpression);
                }
                Console.Write(';');

                break;

            case FunctionCallNode functionCall:
                PrintSyntaxNode(functionCall.Function);

                Console.Write("(");
                for (int i = 0; i < functionCall.Arguments.Length; i++)
                {
                    PrintSyntaxNode(functionCall.Arguments[i]);

                    if (i < functionCall.Arguments.Length - 1)
                    {
                        Console.Write(", ");
                    }
                }
                Console.Write(")");

                break;

            default:
                Console.Write(node);
                break;
        }
    }
}
