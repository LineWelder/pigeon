﻿using System.Reflection;
using System.Text;
using System;

namespace CompilerLibrary.Compiling;

internal class AssemblyGenerator
{
    private readonly StringBuilder textSection = new();
    private readonly StringBuilder dataSection = new();

    private readonly StringBuilder currentFunction = new();

    private int nextLabelId = 0;

    public AssemblyGenerator() { }

    /// <summary>
    /// Returns the assembly symbol corresponding with the given variable name
    /// </summary>
    /// <returns>The assymbly symbol</returns>
    public static string GetAssemblySymbol(string name)
        => $"_{name}";

    /// <summary>
    /// Appends a symbol (label) to the .text section
    /// </summary>
    /// <param name="symbol">The symbol</param>
    public void EmitSymbol(string symbol)
        => textSection.AppendLine($"{symbol}:");

    /// <summary>
    /// Returns the string containing the assembly instruction
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="arguments">The list of instuction arguments</param>
    private static string BuildInstruction(string opcode, params object[] arguments)
    {
        if (arguments.Length > 2)
        {
            throw new ArgumentException("Unexpected number of instruction arguments");
        }

        StringBuilder instruction = new($"\t{opcode}");
        if (arguments.Length == 0)
        {
            return instruction.ToString();
        }

        instruction.Append('\t');
        if (arguments.Length == 1)
        {
            instruction.Append(arguments[0]);
            return instruction.ToString();
        }

        instruction.Append((arguments[0], arguments[1]) switch
        {
            (SymbolValue symbolArgument0, RegisterValue)
                => $"[{symbolArgument0.AddressString}], {arguments[1]}",

            (RegisterValue, SymbolValue symbolArgument1)
                => $"{arguments[0]}, [{symbolArgument1.AddressString}]",

            _   => $"{arguments[0]}, {arguments[1]}"
        });
        return instruction.ToString();
    }

    /// <summary>
    /// Appends an instruction to the .text section
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="arguments">The list of instuction arguments</param>
    public void EmitInstructionToText(string opcode, params object[] arguments)
        => textSection.AppendLine(BuildInstruction(opcode, arguments));

    /// <summary>
    /// Appends an instruction to the current function
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="arguments">The list of instuction arguments</param>
    public void EmitInstruction(string opcode, params object[] arguments)
        => currentFunction.AppendLine(BuildInstruction(opcode, arguments));

    /// <summary>
    /// Creates a new label pointing to the next instruction
    /// </summary>
    public string EmitLabel()
    {
        string label = $".L{nextLabelId++}";
        EmitSymbol(label);
        return label;
    }

    /// <summary>
    /// Appends a variable declaration to the .data section
    /// </summary>
    /// <param name="symbol">The varaible symbol</param>
    /// <param name="declaration">The keyword used for value declaration ('db', 'dw', etc.)</param>
    /// <param name="value">The variable value</param>
    public void EmitVariable(string symbol, string declaration, object value)
        => dataSection.AppendLine($"{symbol} {declaration} {value}");

    /// <summary>
    /// Inserts the generated function code into the text section
    /// </summary>
    public void InsertFunctionCode()
    {
        textSection.Append(currentFunction);
        currentFunction.Clear();
    }

    /// <summary>
    /// Linkes all the separate sections into one assembly code
    /// </summary>
    /// <returns>The generated FASM code</returns>
    public string LinkAssembly()
    {
        Assembly currentAssembly = GetType().Assembly;
        string assemblyTitle = currentAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
                            ?? "null";
        string assemblyVersion = currentAssembly.GetName().Version?.ToString()
                              ?? "null";

        StringBuilder builder = new(
@$"; Generated by {assemblyTitle} version {assemblyVersion}
format PE console
use32
entry _start
include 'include\win32a.inc'"
        );
        
        EmitInstruction("push", "_input");
        EmitInstruction("push", "scanf_format");
        EmitInstruction("call", "[scanf]");
        EmitInstruction("add", "esp", "8");

        EmitInstruction("call", "_main");

        EmitInstruction("push", "eax");
        EmitInstruction("push", "printf_format");
        EmitInstruction("call", "[printf]");
        EmitInstruction("add", "esp", "8");

        EmitInstruction("ret");

        EmitSymbol("_start");
        InsertFunctionCode();

        builder.Append("\n\nsection '.text' readable executable\n\n");
        builder.Append(textSection);

        builder.Append("\nsection '.data' readable writable\n\n");
        builder.Append(dataSection);

        builder.Append("\nsection '.rodata' readable\n\n");
        builder.AppendLine("scanf_format db '%d', 0x00");
        builder.AppendLine("printf_format db '%d', 0x0A, 0x00");

        builder.Append("\nsection '.idata' readable import\n\n");
        builder.AppendLine("library msvcrt, 'msvcrt.dll'");
        builder.AppendLine("import msvcrt, printf, 'printf', scanf, 'scanf'");

        return builder.ToString();
    }
}