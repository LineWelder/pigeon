using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling.Exceptions
{
    public class UnknownIdentifierException : CompilerException
    {
        public IdentifierNode Identifier { get; init; }

        public UnknownIdentifierException(IdentifierNode identifier)
            : base(identifier.Location, $"Unknown identifier {identifier.Value}")
        {
            Identifier = identifier;
        }
    }
}