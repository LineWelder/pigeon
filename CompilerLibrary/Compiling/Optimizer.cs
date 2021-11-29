using System;
using System.Collections.Generic;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

public static class Optimizer
{
    private record struct Mononom(BinaryNodeOperation Operation, SyntaxNode Value);

    /// <summary>
    /// Optimizes the given multiplication or divizion
    /// </summary>
    /// <param name="node">The expression to compute</param>
    private static SyntaxNode OptimizeMononom(BinaryNode node)
    {
        SyntaxNode leftOptimized = OptimizeExpression(node.Left);
        SyntaxNode rightOptimized = OptimizeExpression(node.Right);

        // If we can compute the result
        if (leftOptimized is IntegerNode { Value: long leftValue }
            && rightOptimized is IntegerNode { Value: long rightValue })
        {
            return new IntegerNode(
                node.Location,
                node.Operation switch
                {
                    BinaryNodeOperation.Multiplication => leftValue * rightValue,
                    BinaryNodeOperation.Divizion       => leftValue / rightValue,
                    _ => throw new ArgumentException("Invalid binary operation")
                }
            );
        }

        // Check "a * 1 = a, a * 0 = 0, etc." optimizations
        switch (node.Operation)
        {
            case BinaryNodeOperation.Multiplication:
                long integerOperand;
                SyntaxNode otherOperand;

                if (leftOptimized is IntegerNode { Value: long leftIntegerOperand })
                {
                    integerOperand = leftIntegerOperand;
                    otherOperand = rightOptimized;
                }
                else if (rightOptimized is IntegerNode { Value: long rightIntegerOperand })
                {
                    integerOperand = rightIntegerOperand;
                    otherOperand = leftOptimized;
                }
                else
                {
                    goto default;
                }

                switch (integerOperand)
                {
                    case 0:
                        return new IntegerNode(node.Location, 0);

                    case 1:
                        return otherOperand;
                }
                break;

            case BinaryNodeOperation.Divizion:
                if (leftOptimized is IntegerNode { Value: 0 })
                    return new IntegerNode(node.Location, 0);

                if (rightOptimized is IntegerNode { Value: 1 })
                    return leftOptimized;

                break;

            default:
                return node with
                {
                    Left = leftOptimized,
                    Right = rightOptimized
                };
        }

        return node;
    }

    /// <summary>
    /// Splits the syntax node into mononoms and adds them to the list
    /// add(sub(1, 2), 3) => { { add, 1 }, { sub, 2 }, { add, 3 } }
    /// </summary>
    /// <param name="polynom">The polynom to add to</param>
    /// <param name="node">The syntax node to split</param>
    /// <param name="flipOperations">If true, flips all the operations</param>
    private static void AddToPolynom(List<Mononom> polynom, SyntaxNode node, bool flipOperations)
    {
        switch (node)
        {
            case BinaryNode { Operation: BinaryNodeOperation.Addition
                    or BinaryNodeOperation.Subtraction } binary:
                AddToPolynom(
                    polynom, binary.Left,
                    flipOperations
                );
                AddToPolynom(
                    polynom, binary.Right,
                    flipOperations != binary.Operation is BinaryNodeOperation.Subtraction
                );
                break;

            case NegationNode { InnerExpression: SyntaxNode inner }:
                AddToPolynom(polynom, OptimizeExpression(inner), !flipOperations);
                break;

            default:
                polynom.Add(
                    new Mononom(
                        flipOperations ? BinaryNodeOperation.Subtraction
                                       : BinaryNodeOperation.Addition,
                        OptimizeExpression(node)
                    )
                );
                break;
        }
    }

    /// <summary>
    /// Optimized the given addition or subtraction
    /// </summary>
    /// <param name="node">The expression to optimize</param>
    /// <param name="flipOperations">If true, all the operations will be flipped</param>
    /// <returns>The optimized expression</returns>
    private static SyntaxNode OptimizePolynom(BinaryNode node, bool flipOperations = false)
    {
        List<Mononom> polynom = new();
        AddToPolynom(polynom, node, flipOperations);

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

        int firstPositiveMononomIndex = polynom.FindIndex(x => x.Operation is BinaryNodeOperation.Addition);
        SyntaxNode result;
        
        if (firstPositiveMononomIndex < 0 && constant != 0 || polynom.Count == 0)
        {
            result = new IntegerNode(node.Location, constant);
        }
        else if (firstPositiveMononomIndex < 0)
        {
            result = new NegationNode(polynom[0].Value.Location, polynom[0].Value);
            firstPositiveMononomIndex = 0;
        }
        else
        {
            result = polynom[firstPositiveMononomIndex].Value;
        }

        for (int i = 0; i < polynom.Count; i++)
        {
            if (i == firstPositiveMononomIndex)
                continue;

            result = new BinaryNode(
                result.Location,
                polynom[i].Operation,
                Left: result,
                Right: polynom[i].Value
            );
        }

        if (constant != 0 && firstPositiveMononomIndex >= 0)
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

    /// <summary>
    /// Optimizes the given expression
    /// </summary>
    /// <param name="node">The expression to optimize</param>
    /// <returns>The optimized expression</returns>
    public static SyntaxNode OptimizeExpression(SyntaxNode node)
    {
        switch (node)
        {
            case TypeCastNode typeCast:
                return typeCast with { Value = OptimizeExpression(typeCast.Value) };

            case BinaryNode binary:
                if (binary.Operation is BinaryNodeOperation.Addition or BinaryNodeOperation.Subtraction)
                    return OptimizePolynom(binary);
                else
                    return OptimizeMononom(binary);

            case NegationNode negation:
                SyntaxNode innerOptimized = OptimizeExpression(negation.InnerExpression);

                if (innerOptimized is IntegerNode innerInteger)
                {
                    return new IntegerNode(
                        node.Location,
                        -innerInteger.Value
                    );
                }
                else if (innerOptimized is BinaryNode { Operation: BinaryNodeOperation.Addition
                    or BinaryNodeOperation.Subtraction } innerBinary)
                {
                    return OptimizePolynom(innerBinary, true);
                }
                else
                {
                    return negation with { InnerExpression = innerOptimized };
                }

            default:
                return node;
        };
    }
}