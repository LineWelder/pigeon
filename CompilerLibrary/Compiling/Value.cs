namespace CompilerLibrary.Compiling;

internal record Value(CompiledType Type)
{
    public virtual bool IsRValue => false;
}

internal record SymbolValue(CompiledType Type, string Symbol)
    : Value(Type)
{
    public override bool IsRValue => true;
}

internal record IntegerValue(CompiledType Type, long Value)
    : Value(Type);