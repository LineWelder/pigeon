using CompilerLibrary.Compiling;
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
                Console.Write(' ');
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

                for (int i = 0; i < functionDeclaration.ArgumentList.Length; i++)
                {
                    PrintSyntaxNode(functionDeclaration.ArgumentList[0].Type);
                    Console.Write($" {functionDeclaration.ArgumentList[0].Identifier}");
                    if (i < functionDeclaration.ArgumentList.Length - 1)
                        Console.Write(", ");
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
                if (typeCast.Value is BinaryNode) Console.Write('(');
                PrintSyntaxNode(typeCast.Value);
                if (typeCast.Value is BinaryNode) Console.Write(')');

                Console.Write(':');
                PrintSyntaxNode(typeCast.Type);

                break;

            case NegationNode negation:
                Console.Write('-');
                if (negation.InnerExpression is not (IntegerNode or IdentifierNode)) Console.Write('(');
                PrintSyntaxNode(negation.InnerExpression);
                if (negation.InnerExpression is not (IntegerNode or IdentifierNode)) Console.Write(')');

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

                if (leftParentheses) Console.Write('(');
                PrintSyntaxNode(binary.Left);
                if (leftParentheses) Console.Write(')');

                Console.Write($" {BINARY_OPERATORS[binary.Operation]} ");

                if (rightParentheses) Console.Write('(');
                PrintSyntaxNode(binary.Right);
                if (rightParentheses) Console.Write(')');

                break;

            case AssignmentNode assignment:
                PrintSyntaxNode(assignment.Left);
                Console.Write(" = ");
                PrintSyntaxNode(assignment.Right);
                Console.Write(';');

                break;

            case ReturnNode @return:
                Console.Write("return ");
                PrintSyntaxNode(@return.InnerExpression);
                Console.Write(';');

                break;

            case FunctionCallNode functionCall:
                PrintSyntaxNode(functionCall.Function);
                Console.Write("()");
                break;

            default:
                Console.Write(node);
                break;
        }
    }
}
