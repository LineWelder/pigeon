using System.Collections.Generic;

namespace CompilerLibrary.Compiling;

/// <summary>
/// The representation of a type used by the compiler
/// Size is given in bytes
/// </summary>
public record TypeInfo(
    uint Size, string Name, bool IsSigned
)
{
    private static readonly Dictionary<uint, string> ASSEMBLY_TYPES = new()
    {
        { 1, "byte" },
        { 2, "word" },
        { 4, "dword" }
    };

    /// <summary>
    /// Returns the value of <paramref name="length"/> ones
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
    /// The value of Size bytes filled with ones
    /// </summary>
    public long Mask => Ones(8 * Size);

    /// <summary>
    /// The maximum value that can be stored in a variable of this type
    /// </summary>
    public long MaximumValue => Ones(8 * Size - (IsSigned ? 1u : 0u));

    /// <summary>
    /// The maximum value that can be stored in a variable of this type
    /// </summary>
    public long MinimumValue => IsSigned ? -MaximumValue - 1 : 0;

    /// <summary>
    /// The name of the type used in assembly
    /// </summary>
    public string AssemblyName => ASSEMBLY_TYPES[Size];

    /// <summary>
    /// The keyword used to declare a value of the type in assembly
    /// </summary>
    public string AssemblyDeclaration => $"d{ASSEMBLY_TYPES[Size][0]}";
}

public record FunctionPointerTypeInfo(FunctionInfo FunctionInfo)
    : TypeInfo(Size: 4, Name: $"{FunctionInfo.ReturnType?.Name}@()", IsSigned: false);