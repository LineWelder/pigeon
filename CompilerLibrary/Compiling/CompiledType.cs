namespace CompilerLibrary.Compiling;

/// <summary>
/// The representation of a type used by the compiler
/// Size is given in bytes
/// </summary>
public record CompiledType(
    uint Size,
    string Declaration, string Name,
    char Abbreviation, bool IsSigned
)
{
    /// <summary>
    /// Represents the maximum value that can be stored in a variable of this type
    /// </summary>
    public long MaximumValue
    {
        get
        {
            if (Size == 0) return 0;

            int bitCount = (int)(8 * Size - (IsSigned ? 1 : 0));
            long temp = 1;
            for (int i = 0; i < bitCount; i++)
                temp <<= 1;

            return --temp;
        }
    }

    /// <summary>
    /// Represents the maximum value that can be stored in a variable of this type
    /// </summary>
    public long MinimumValue => IsSigned ? -MaximumValue - 1 : 0; 
}