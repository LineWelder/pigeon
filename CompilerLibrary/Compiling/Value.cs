namespace CompilerLibrary.Compiling;

internal record Value(CompiledType Type)
{
    public virtual bool IsLValue => false;
}

internal record SymbolValue(CompiledType Type, string Symbol)
    : Value(Type)
{
    public override bool IsLValue => true;
    public override string ToString() => $"{Type.Name} ptr [{Symbol}]";
}

internal record IntegerValue(CompiledType Type, long Value)
    : Value(Type)
{
    public override string ToString() => Value.ToString();
}