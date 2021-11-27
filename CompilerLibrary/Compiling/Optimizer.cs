﻿using System;
using System.Linq;
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
        List<Mononom> polynom, SyntaxNode syntaxNode, bool flipOperations
    )
    {
        if (syntaxNode is BinaryNode binary
         && binary.Operation is BinaryNodeOperation.Addition or BinaryNodeOperation.Subtraction)
        {
            AddToThePolynom(
                polynom, binary.Left,
                flipOperations
            );
            AddToThePolynom(
                polynom, binary.Right,
                Xor(flipOperations, binary.Operation is BinaryNodeOperation.Subtraction)
            );
        }
        else
        {
            polynom.Add(
                new Mononom(
                    flipOperations ? BinaryNodeOperation.Subtraction : BinaryNodeOperation.Addition,
                    syntaxNode
                )
            );
        }
    }

    /// <summary>
    /// Takes a syntax node and returns a list of values packed with
    /// the operation they used with
    /// add(sub(1, 2), 3) => { { add, 1 }, { sub, 2 }, { add, 3 } }
    /// </summary>
    /// <param name="syntaxNode">The node to split</param>
    private static List<Mononom> SplitIntoPolynom(SyntaxNode syntaxNode)
    {
        List<Mononom> result = new();
        AddToThePolynom(result, syntaxNode, false);
        return result;
    }

    /// <summary>
    /// Optimizes the given expression
    /// </summary>
    /// <param name="node">The expression to optimize</param>
    /// <returns>The optimized expression</returns>
    public static SyntaxNode OptimizeExpression(SyntaxNode node)
    {
        List<Mononom> polynom = SplitIntoPolynom(node);

        // Sum up all the constants

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

        // Convert the polynome back to SyntaxNode

        int firstMononomIndex = polynom.FindIndex(x => x.Operation is BinaryNodeOperation.Addition);
        SyntaxNode result = firstMononomIndex >= 0
            ? polynom[firstMononomIndex].Value
            : new IntegerNode(node.Location, constant);

        for (int i = 0; i < polynom.Count; i++)
        {
            if (i == firstMononomIndex)
                continue;

            result = new BinaryNode(
                result.Location,
                polynom[i].Operation,
                Left: result,
                Right: polynom[i].Value
            );
        }

        if (constant != 0 && firstMononomIndex >= 0)
        {
            result = new BinaryNode(
                result.Location,
                constant > 0 ? BinaryNodeOperation.Addition : BinaryNodeOperation.Subtraction,
                Left: result,
                Right: new IntegerNode(node.Location, Math.Abs(constant))
            );
        }

        return result;
    }
}