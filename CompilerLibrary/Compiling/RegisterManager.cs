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
    /// The id of the register used to return values from functions
    /// </summary>
    public const int RETURN_REGISTER_ID = 0;

    /// <summary>
    /// Which registers were used in the current function
    /// </summary>
    private readonly bool[] used = new bool[4];

    /// <summary>
    /// The element of index x is the allocation id of the value stored
    /// in the register of id x, or -1 if the register is free
    /// </summary>
    private readonly int[] allocations = new int[4];

    public RegisterManager()
    {
        Array.Fill(allocations, -1);
    }

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
        {
            throw new ArgumentException($"{id} is not a register id", nameof(id));
        }

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
    /// Returns the register which hold the value's name
    /// </summary>
    public int GetRegisterIdFromAllocation(RegisterValue value)
    {
        // It is the return register
        if (value.AllocationId == -2)
        {
            return RETURN_REGISTER_ID;
        }

        int registerId = Array.IndexOf(allocations, value.AllocationId);
        if (registerId < 0)
        {
            throw new ArgumentException($"The allocation of id {value.AllocationId} does not exist");
        }

        return registerId;
    }

    /// <summary>
    /// Returns a not-used register and marks it as used
    /// </summary>
    /// <param name="node">The node which needs the allocated register, used for throwing OutOfRegistersException</param>
    /// <returns>A free register</returns>
    public RegisterValue AllocateRegister(SyntaxNode node, TypeInfo type)
    {
        int id = Array.FindIndex(allocations, x => x < 0);
        if (id < 0)
        {
            throw new OutOfRegistersException(node.Location);
        }

        used[id] = true;
        allocations[id] = allocations.Max() + 1;
        return new RegisterValue(type, this, allocations[id]);
    }

    /// <summary>
    /// Allocates the required register, if it was not free, moves the previous allocation
    /// to another register and returns its id for mov generation
    /// </summary>
    /// <param name="node">The node which needs the allocated register, used for throwing OutOfRegistersException</param>
    /// <param name="id">The id of the required register</param>
    public (RegisterValue register, int oldValueNewRegister) RequireRegister(
        SyntaxNode node, TypeInfo type, int id)
    {
        int oldValueNewRegister = -1;

        if (allocations[id] >= 0)
        {
            oldValueNewRegister = Array.FindIndex(allocations, x => x < 0);
            if (oldValueNewRegister <= 0)
            {
                throw new OutOfRegistersException(node.Location);
            }

            used[id] = true;
            allocations[oldValueNewRegister] = allocations[id];
        }

        used[id] = true;
        allocations[id] = allocations.Max() + 1;
        return (
            new RegisterValue(type, this, allocations[id]),
            oldValueNewRegister
        );
    }

    /// <summary>
    /// Returns the register used for returning values from functions,
    /// but does not allocate it. Is needed to specify the destination when compiling
    /// return statement
    /// </summary>
    public RegisterValue GetReturnRegister(TypeInfo type)
    {
        if (allocations[0] >= 0)
        {
            throw new Exception("eax is not freed");
        }

        return new RegisterValue(type, this, -2);
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
        {
            return;
        }

        try
        {
            int id = GetRegisterIdFromAllocation(register);
            allocations[id] = -1;
        }
        catch (ArgumentException) { }
    }
}