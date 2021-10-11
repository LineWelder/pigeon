namespace CompilerLibrary.Compiling
{
    /// <summary>
    /// Represents a function argument
    /// </summary>
    public record FunctionArgument(Location SourceLocation, CompiledType Type, string Name);

    /// <summary>
    /// Represents a compiled function
    /// </summary>
    public record CompiledFunction(
        Location SourceLocation,
        string AssemblySymbol,
        CompiledType ReturnType, FunctionArgument[] Arguments,
        string AssemblyCode
    );
}
