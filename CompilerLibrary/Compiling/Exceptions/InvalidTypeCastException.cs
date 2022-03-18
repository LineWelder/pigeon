namespace CompilerLibrary.Compiling.Exceptions;

public class InvalidTypeCastException : CompilerException
{
    public TypeInfo? FromType { get; init; }
    public TypeInfo ToType { get; init; }

    public InvalidTypeCastException(
        Location location, TypeInfo? fromType, TypeInfo toType, string error)
        : base(location, $"Invalid type cast from type {fromType?.Name ?? "integer"} to type {toType.Name}, {error}")
    {
        FromType = fromType;
        ToType = toType;
    }
}