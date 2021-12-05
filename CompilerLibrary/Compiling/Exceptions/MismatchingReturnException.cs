namespace CompilerLibrary.Compiling.Exceptions;

public class MismatchingReturnException : CompilerException
{
    public TypeInfo? ExpectedType { get; init; }

    public MismatchingReturnException(Location location, TypeInfo? expectedType)
        : base(location, $"Mismatching return statement, {expectedType?.Name ?? "no value"} expected")
    {
        ExpectedType = expectedType;
    }
}