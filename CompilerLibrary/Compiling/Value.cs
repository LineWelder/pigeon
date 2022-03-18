namespace CompilerLibrary.Compiling;

internal record Value(TypeInfo? Type);

/// <summary>
/// Represents a value with a type that doesn't depend on the context
/// </summary>
internal record StronglyTypedValue(TypeInfo Type)
    : Value(Type)
{
    public TypeInfo StrongType => Type!;
}

internal record SymbolValue(TypeInfo Type, string Symbol)
    : StronglyTypedValue(Type)
{
    public override string ToString()
        => Type is FunctionPointerTypeInfo
               ? Symbol : $"{Type!.AssemblyName} [{Symbol}]";
}

/// <param name="RegisterManager">The register manager managing the value</param>
/// <param name="AllocationId">The id of the allocation, not the register id</param>
internal record RegisterValue(TypeInfo Type, RegisterManager RegisterManager, int AllocationId)
    : StronglyTypedValue(Type)
{
    public string Name => RegisterManager.GetRegisterNameFromId(
        RegisterManager.GetRegisterIdFromAllocation(this),
        Type!
    );

    public override string ToString() => Name;
}

internal record IntegerValue(TypeInfo? Type, long Value)
    : Value(Type)
{
    public override string ToString() => Value.ToString();
}