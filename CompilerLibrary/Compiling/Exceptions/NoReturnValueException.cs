namespace CompilerLibrary.Compiling.Exceptions;

public class NoReturnValueException : CompilerException
{
    public string FunctionType { get; init; }

    public NoReturnValueException(Location location, string functionType)
        : base(location, $"The function of type {functionType} does not return any value")
    {
        FunctionType = functionType;
    }
}