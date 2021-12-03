using System;
using System.Collections.Generic;
using System.Linq;

using CompilerLibrary.Compiling.Exceptions;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

/// <summary>
/// Manages the usage of registers
/// </summary>
internal class RegisterManager
{
    /// <summary>
    /// Which registers were used in the current function
    /// </summary>
    private readonly bool[] used = new bool[4];

    /// <summary>
    /// Which registers are currently allocated
    /// </summary>
    private readonly bool[] allocated = new bool[4];

    /// <summary>
    /// Returns the enumerable of the registers used in the current function
    /// </summary>
    public IEnumerable<string> Used
        => from id in Enumerable.Range(1, used.Length - 1)
           where used[id]
           select GetRegisterNameFromId(id, Compiler.COMPILED_TYPES["i32"]);

    /// <summary>
    /// Returns the name of the register of the given size corresponding to the given id
    /// </summary>
    public static string GetRegisterNameFromId(int id, TypeInfo type)
    {
        if (id is < 0 or > 3)
            throw new ArgumentException($"{id} is not a register id", nameof(id));

        char registerChar = (char)(id + 'a');

        return type.Size switch
        {
            1 => $"{registerChar}l",
            2 => $"{registerChar}x",
            4 => $"e{registerChar}x",
            _ => throw new ArgumentException($"Invalid type size {type.Size}"),
        };
    }

    /// <summary>
    /// Returns the name of the register corresponding to the given id
    /// </summary>
    public static int GetRegisterIdFromName(string name)
    {
        int id = name[ name[0] is 'e' ? 1 : 0 ] - 'a';
        if (id is < 0 or > 3)
            throw new ArgumentException($"{name} is not a register", nameof(name));

        return id;
    }

    /// <summary>
    /// Returns a not-used register and marks it as used
    /// </summary>
    /// <param name="node">The node which needs the allocated register, used for throwing OutOfRegistersException</param>
    /// <returns>A free register</returns>
    public RegisterValue AllocateRegister(SyntaxNode node, TypeInfo type)
    {
        int id = 0;
        while (allocated[id])
        {
            id++;

            if (id > 4)
            {
                throw new OutOfRegistersException(node.Location);
            }
        }

        used[id] = true;
        allocated[id] = true;
        return new RegisterValue(type, GetRegisterNameFromId(id, type));
    }

    /// <summary>
    /// Should be called in the beginning of a function compilation
    /// Makes the manager forget which registers were used
    /// </summary>
    public void ResetUsedRegisters()
    {
        Array.Fill(used, false);
    }

    /// <summary>
    /// Marks the given register as free
    /// </summary>
    /// <param name="value">The register to free</param>
    public void FreeRegister(Value value)
    {
        if (value is not RegisterValue register)
            return;

        int id = GetRegisterIdFromName(register.Name);
        allocated[id] = false;
    }
}