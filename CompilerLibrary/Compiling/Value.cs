namespace CompilerLibrary.Compiling;

internal record Value(TypeInfo Type);

internal record SymbolValue(TypeInfo Type, string Symbol)
    : Value(Type)
{
    public override string ToString()
        => Type is FunctionPointerTypeInfo
               ? Symbol : $"{Type.AssemblyName} [{Symbol}]";
}

/// <param name="RegisterManager">The register manager managing the value</param>
/// <param name="AllocationId">The id of the allocation, not the register id</param>
internal record RegisterValue(TypeInfo Type, RegisterManager RegisterManager, int AllocationId)
    : Value(Type)
{
    public string Name => RegisterManager.GetRegisterNameFromId(
        RegisterManager.GetRegisterIdFromAllocation(this),
        Type
    );

    public override string ToString() => Name;
}

internal record IntegerValue(TypeInfo Type, long Value)
    : Value(Type)
{
    public override string ToString() => Value.ToString();
}