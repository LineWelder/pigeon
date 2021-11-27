namespace CompilerLibrary.Compiling;

internal record Value(CompiledType Type)
{
    public virtual bool IsLValue => false;
}

internal record SymbolValue(CompiledType Type, string Symbol)
    : Value(Type)
{
    public override bool IsLValue => true;
}

internal record RegisterValue(CompiledType Type, string Name)
    : Value(Type)
{
    public override bool IsLValue => true;
}

internal record IntegerValue(CompiledType Type, long Value)
    : Value(Type);