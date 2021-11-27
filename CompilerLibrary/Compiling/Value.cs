﻿namespace CompilerLibrary.Compiling;

internal record Value(CompiledType Type);

internal record SymbolValue(CompiledType Type, string Symbol)
    : Value(Type)
{
    public override string ToString() => $"{Type.Name} ptr [{Symbol}]";
}

internal record RegisterValue(CompiledType Type, string Name)
    : Value(Type)
{
    public override string ToString() => Name;
}

internal record IntegerValue(CompiledType Type, long Value)
    : Value(Type)
{
    public override string ToString() => Value.ToString();
}