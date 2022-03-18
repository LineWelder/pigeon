namespace CompilerLibrary.Compiling.Exceptions;

public class NotCallableTypeException : CompilerException
{
    public TypeInfo? Type { get; init; }

    public NotCallableTypeException(Location location, TypeInfo? type)
        : base(location, $"{type?.Name ?? "integer"} type is not callable")
    {
        Type = type;
    }
}