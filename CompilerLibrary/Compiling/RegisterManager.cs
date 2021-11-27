using CompilerLibrary.Compiling.Exceptions;
using CompilerLibrary.Parsing;
using System;

namespace CompilerLibrary.Compiling;

/// <summary>
/// Manages the usage of registers
/// </summary>
internal class RegisterManager
{
    private static readonly string[] REGISTER_NAMES = {
        "eax", "ebx", "ecx", "edx"
    };

    private readonly bool[] allocated = new bool[REGISTER_NAMES.Length];

    /// <summary>
    /// Returns the name of the register corresponding to the given id
    /// </summary>
    private static string GetRegisterNameFromId(int id)
        => REGISTER_NAMES[id];

    /// <summary>
    /// Returns the name of the register corresponding to the given id
    /// </summary>
    private static int GetRegisterIdFromName(string name)
    {
        int id = name[1] - 'a';
        if (id < 0 || id > REGISTER_NAMES.Length)
        {
            throw new ArgumentException($"{name} is not a register", nameof(name));
        }

        return id;
    }

    /// <summary>
    /// Returns a not-used register and marks it as used
    /// </summary>
    /// <param name="node">The node which needs the allocated register, used for throwing OutOfRegistersException</param>
    /// <returns>A free register</returns>
    public RegisterValue AllocateRegister(SyntaxNode node)
    {
        int id = 0;
        while (allocated[id])
        {
            id++;

            if (id > REGISTER_NAMES.Length)
            {
                throw new OutOfRegistersException(node.Location);
            }
        }

        allocated[id] = true;
        return new RegisterValue(Compiler.COMPILED_TYPES["i32"], GetRegisterNameFromId(id));
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