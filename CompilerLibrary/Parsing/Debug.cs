using System;
using System.Collections.Generic;

namespace CompilerLibrary.Parsing;

public static class Debug
{
    private static readonly Dictionary<BinaryNodeOperation, char> BINARY_OPERATORS = new()
    {
        { BinaryNodeOperation.Addition, '+' },
        { BinaryNodeOperation.Subtraction, '-' },
        { BinaryNodeOperation.Multiplication, '*' },
        { BinaryNodeOperation.Divizion, '/' },
    };

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
                Console.Write($" {BINARY_OPERATORS[binary.Operation]} ");
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
