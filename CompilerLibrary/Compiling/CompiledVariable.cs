namespace CompilerLibrary.Compiling;

/// <summary>
/// Represents a compiled variable
/// </summary>
public record CompiledVariable(
    Location SourceLocation,
    string AssemblySymbol,
    CompiledType Type,
    string AssemblyValue
);
