namespace CompilerLibrary.Compiling;

/// <summary>
/// The representation of a type used by the compiler
/// Size is given in bytes
/// </summary>
public record TypeInfo(
    uint Size,
    string Declaration, string Name,
    char Abbreviation, bool IsSigned
)
{
    /// <summary>
    /// Returns the value of length ones
    /// </summary>
    /// <param name="length">The amount of ones</param>
    private static long Ones(uint length)
    {
        long temp = 0;
        for (int i = 0; i < length; i++)
            temp |= 1L << i;

        return temp;
    }

    /// <summary>
    /// Returns the value of Size bytes filled with ones
    /// </summary>
    public long Mask => Ones(8 * Size);

    /// <summary>
    /// Represents the maximum value that can be stored in a variable of this type
    /// </summary>
    public long MaximumValue => Ones(8 * Size - (IsSigned ? 1u : 0u));

    /// <summary>
    /// Represents the maximum value that can be stored in a variable of this type
    /// </summary>
    public long MinimumValue => IsSigned ? -MaximumValue - 1 : 0;
}