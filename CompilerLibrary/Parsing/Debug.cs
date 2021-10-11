using System;
using System.Collections.Generic;

namespace CompilerLibrary.Parsing
{
    public static class Debug
    {
        private static readonly Dictionary<BinaryNodeOperation, char> BINARY_OPERATORS= new()
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
                    PrintSyntaxNode(functionDeclaration.ReturnType);
                    Console.Write($" {functionDeclaration.Identifier}(");

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
                        PrintSyntaxNode(statement, offset + 4);
                    Console.Write("\n}");

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

                case BinaryNode binaryNode:
                    Console.Write('(');
                    PrintSyntaxNode(binaryNode.Left);
                    Console.Write($" {BINARY_OPERATORS[binaryNode.Operation]} ");
                    PrintSyntaxNode(binaryNode.Right);
                    Console.Write(')');

                    break;

                default:
                    Console.Write(node);
                    break;
            }
        }
    }
}
