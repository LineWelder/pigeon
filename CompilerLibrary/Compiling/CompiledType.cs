namespace CompilerLibrary.Compiling
{
    /// <summary>
    /// The representation of a type used by the compiler
    /// Size is given in bytes
    /// </summary>
    public record CompiledType(uint Size, string Declaration, char Abbreviation);
}
