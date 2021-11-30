namespace CompilerLibrary.Compiling;

internal record Value(TypeInfo Type);

internal record SymbolValue(TypeInfo Type, string Symbol)
    : Value(Type)
{
    public override string ToString() => $"{Type.Name} ptr [{Symbol}]";
}

internal record RegisterValue(TypeInfo Type, string Name)
    : Value(Type)
{
    public override string ToString() => Name;
}

internal record IntegerValue(TypeInfo Type, long Value)
    : Value(Type)
{
    public override string ToString() => Value.ToString();
}