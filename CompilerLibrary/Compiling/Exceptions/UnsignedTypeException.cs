namespace CompilerLibrary.Compiling.Exceptions;

public class UnsignedTypeException : CompilerException
{
    public TypeInfo Type { get; init; }

    public UnsignedTypeException(Location location, TypeInfo type, string error)
        : base(location, $"{type.Name} type is unsigned, {error}")
    {
        Type = type;
    }
}