﻿using CompilerLibrary.Parsing;
using System.Reflection;
using System.Text;

namespace CompilerLibrary.Compiling;

internal class AssemblyGenerator
{
    private readonly StringBuilder textSection = new();
    private readonly StringBuilder dataSection = new();

    public AssemblyGenerator() { }

    /// <summary>
    /// Returns the assembly symbol corresponding with the given variable name
    /// </summary>
    /// <returns>The assymbly symbol</returns>
    public static string GetAssemblySymbol(string name)
        => $"_{name}";

    /// <summary>
    /// Returns the assembly symbol for function declaration
    /// </summary>
    /// <returns></returns>
    public static string GetAssemblySymbol(FunctionDeclarationNode function)
    {
        StringBuilder builder = new(
            "_", 2 + function.Identifier.Length + function.ArgumentList.Length
        );

        builder.Append(function.Identifier);
        if (function.ArgumentList.Length > 0)
            builder.Append('@');
        foreach (FunctionArgumentDeclarationNode argument in function.ArgumentList)
            builder.Append(Compiler.GetTypeInfo(argument.Type).Abbreviation);

        return builder.ToString();
    }

    /// <summary>
    /// Appends a symbol (label) to the .text section
    /// </summary>
    /// <param name="symbol">The symbol</param>
    public void EmitSymbol(string symbol)
        => textSection.AppendLine($"{symbol}:");

    /// <summary>
    /// Appends an instruction to the .text section
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="arguments">The list of instuction arguments</param>
    public void EmitInstruction(string opcode, params object[] arguments)
        => textSection.AppendLine($"\t{opcode}\t{string.Join(", ", arguments)}");

    /// <summary>
    /// Appends a variable declaration to the .data section
    /// </summary>
    /// <param name="symbol">The varaible symbol</param>
    /// <param name="declaration">The keyword used for value declaration ('db', 'dw', etc.)</param>
    /// <param name="value">The variable value</param>
    public void EmitVariable(string symbol, string declaration, object value)
        => dataSection.AppendLine($"{symbol} {declaration} {value}");

    /// <summary>
    /// Linkes all the separate sections into one assembly code
    /// </summary>
    /// <returns>The generated FASM code</returns>
    public string LinkAssembly()
    {
        Assembly currentAssembly = GetType().Assembly;
        string assemblyTitle = currentAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        string assemblyVersion = currentAssembly.GetName().Version.ToString();

        StringBuilder builder = new(
@$"; Generated by {assemblyTitle} version {assemblyVersion}
format PE console
use32"
        );

        builder.Append("\n\nsection '.text' readable executable\n\n");
        builder.Append(textSection);

        builder.Append("\n\nsection '.data' readable writable\n\n");
        builder.Append(dataSection);

        return builder.ToString();
    }
}