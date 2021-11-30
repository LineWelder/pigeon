using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

/// <summary>
/// Represents a function argument
/// </summary>
public record FunctionArgument(Location SourceLocation, TypeInfo Type, string Name);

/// <summary>
/// Represents a compiled function
/// </summary>
public record FunctionInfo(
    Location SourceLocation,
    string AssemblySymbol,
    TypeInfo? ReturnType, FunctionArgument[] Arguments,
    SyntaxNode[] Body
);