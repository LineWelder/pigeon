using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling.Exceptions
{
    public class UnexpectedSyntaxNodeException : CompilerException
    {
        public SyntaxNode Node { get; init; }

        public UnexpectedSyntaxNodeException(SyntaxNode node, string expectation)
            : base(node.Location, $"Unexpected syntax node, {expectation} expected")
        {
            Node = node;
        }
    }
}
