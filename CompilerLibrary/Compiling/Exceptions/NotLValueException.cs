using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling.Exceptions;

public class NotLValueException : CompilerException
{
    public SyntaxNode Node { get; init; }

    public NotLValueException(SyntaxNode node)
        : base(node.Location, $"Expected lvalue")
    {
        Node = node;
    }
}