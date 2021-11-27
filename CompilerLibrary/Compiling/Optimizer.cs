using System;
using System.Collections.Generic;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

public static class Optimizer
{
    private record struct Mononom(BinaryNodeOperation Operation, SyntaxNode Value);

    private static bool Xor(bool a, bool b)
        => a && !b || !a && b;

    /// <summary>
    /// Splits the syntax node into mononoms and adds them to the list
    /// </summary>
    /// <param name="polynom">The polynom to add to</param>
    /// <param name="syntaxNode">The syntax node to split</param>
    /// <param name="firstOperation">The operation used for the first mononom</param>
    /// <param name="flipOperations">If true, flips all the operations</param>
    private static void AddToThePolynom(
        List<Mononom> polynom, SyntaxNode syntaxNode,
        BinaryNodeOperation firstOperation, bool flipOperations
    )
    {
        if (syntaxNode is BinaryNode binary
         && binary.Operation is BinaryNodeOperation.Addition or BinaryNodeOperation.Subtraction)
        {
            BinaryNodeOperation operation = binary.Operation;
            if (flipOperations)
            {
                operation = 1 - operation;
            }

            AddToThePolynom(polynom, binary.Left, firstOperation, flipOperations);
            AddToThePolynom(
                polynom, binary.Right, operation,
                Xor(flipOperations, operation is BinaryNodeOperation.Subtraction)
            );
        }
        else
        {
            polynom.Add(new Mononom(firstOperation, syntaxNode));
        }
    }

    /// <summary>
    /// Takes a syntax node and returns a list of values packed with
    /// the operation they used with. The first value's operation doesn't
    /// mean anything
    /// add(sub(1, 2), 3) => { { ?, 1 }, { sub, 2 }, { add, 3 } }
    /// </summary>
    /// <param name="syntaxNode">The node to split</param>
    /// <returns></returns>
    private static List<Mononom> SplitIntoPolynom(SyntaxNode syntaxNode)
    {
        List<Mononom> result = new();
        AddToThePolynom(result, syntaxNode, 0, false);
        return result;
    }

    /// <summary>
    /// Converts a polynom back into a syntax node
    /// </summary>
    /// <param name="polynom">The polynom to convert</param>
    private static SyntaxNode ConvertIntoSyntaxNode(Span<Mononom> polynom)
        => polynom.Length > 1
            ? new BinaryNode(
                polynom[0].Value.Location,
                polynom[^1].Operation,
                Left: ConvertIntoSyntaxNode(polynom[..^1]),
                Right: polynom[^1].Value
            )
            : polynom[0].Value;

    /// <summary>
    /// Optimizes the given expression
    /// </summary>
    /// <param name="syntaxNode">The expression to optimize</param>
    /// <returns>The optimized expression</returns>
    public static SyntaxNode OptimizeExpression(SyntaxNode syntaxNode)
    {
        List<Mononom> polynom = SplitIntoPolynom(syntaxNode);

        long constant = 0;
        for (int i = polynom.Count - 1; i >= 0; i--)
        {
            if (polynom[i].Value is not IntegerNode { Value: long value })
                continue;

            switch (polynom[i].Operation)
            {
                case BinaryNodeOperation.Addition:
                    constant += value;
                    break;

                case BinaryNodeOperation.Subtraction:
                    constant -= value;
                    break;

                default:
                    throw new ArgumentException("Invalid binary node operation");
            }

            polynom.RemoveAt(i);
        }

        if (constant != 0)
        {
            polynom.Add(new Mononom(
                constant < 0 ? BinaryNodeOperation.Subtraction : BinaryNodeOperation.Addition,
                new IntegerNode(syntaxNode.Location, Math.Abs(constant))
            ));
        }

        return ConvertIntoSyntaxNode(polynom.ToArray().AsSpan());
    }
}