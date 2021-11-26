namespace CompilerLibrary.Compiling.Exceptions;

public class InvalidTypeCastException : CompilerException
{
    public string FromType { get; init; }
    public string ToType { get; init; }

    public InvalidTypeCastException(Location location, string fromType, string toType, string error)
        : base(location, $"Invalid type cast from type {fromType} to type {toType}, {error}")
    {
        FromType = fromType;
        ToType = toType;
    }
}