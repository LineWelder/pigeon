﻿using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CompilerLibrary.Compiling.Exceptions;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

/// <summary>
/// Generates FASM code based on parsed Pigeon
/// </summary>
public class Compiler
{
    private static readonly Dictionary<string, CompiledType> COMPILED_TYPES = new()
    {
        { "i32", new CompiledType(Size: 4, Declaration: "dd", Name: "dword", Abbreviation: 'i', IsSigned: true) }
    };

    public readonly Dictionary<string, CompiledVariable> variables = new();
    public readonly Dictionary<string, CompiledFunction> functions = new();

    public Compiler() { }

    /// <summary>
    /// Returns the corresponding CompiledType for the given type
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>The corresponding compiled type</returns>
    public static CompiledType GetCompiledType(SyntaxNode type)
    {
        if (type is not IdentifierNode identifier)
            throw new UnexpectedSyntaxNodeException(type, "type identifier");

        if (COMPILED_TYPES.TryGetValue(identifier.Value, out CompiledType compiledType))
            return compiledType;

        throw new UnknownIdentifierException(identifier);
    }

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
            builder.Append(GetCompiledType(argument.Type).Abbreviation);

        return builder.ToString();
    }

    /// <summary>
    /// Adds a function to the declared functions list
    /// </summary>
    /// <param name="function">The function to register</param>
    public void RegisterFunction(FunctionDeclarationNode function)
    {
        FunctionArgument[] arguments = new FunctionArgument[function.ArgumentList.Length];
        for (int i = 0; i < function.ArgumentList.Length; i++)
        {
            FunctionArgumentDeclarationNode argument = function.ArgumentList[i];
            arguments[i] = new FunctionArgument(
                argument.Location,
                GetCompiledType(argument.Type),
                argument.Identifier
            );
        }

        string assemblySymbol = GetAssemblySymbol(function);

        functions.Add(assemblySymbol, new CompiledFunction(
            function.Location,
            assemblySymbol,
            function.ReturnType is null ? null : GetCompiledType(function.ReturnType),
            arguments,
            function.Body
        ));
    }

    /// <summary>
    /// Adds a global variable to the declared variables list
    /// </summary>
    /// <param name="variable">The variable declaration</param>
    public void RegisterVariable(VariableDeclarationNode variable)
    {
        CompiledType variableType = GetCompiledType(variable.Type);
        if (variable.Value is not IntegerNode valueInteger)
            throw new UnexpectedSyntaxNodeException(variable.Value, "a number");

        long maximumValue = variableType.MaximumValue;
        if (valueInteger.Value > maximumValue)
            throw new InvalidTypeCastException(
                variable.Value.Location,
                "bigger integer type",
                variableType.ToString(),
                $"the definition value must not be greater than {maximumValue}"
            );

        string assemblySymbol = GetAssemblySymbol(variable.Identifier);
        string variableValue = valueInteger.Value.ToString();

        variables.Add(assemblySymbol, new CompiledVariable(
            variable.Location,
            assemblySymbol,
            variableType,
            variableValue
        ));
    }

    /// <summary>
    /// Compiles the given nodes
    /// </summary>
    public void CompileNodes(SyntaxNode[] nodes)
    {
        foreach (SyntaxNode node in nodes)
        {
            switch (node)
            {
                case VariableDeclarationNode variable:
                    RegisterVariable(variable);
                    break;

                case FunctionDeclarationNode function:
                    RegisterFunction(function);
                    break;

                default:
                    throw new UnexpectedSyntaxNodeException(
                        node, "variable or function declaration"
                    );
            }
        }
    }

    /// <summary>
    /// Compiels an expression and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="builder">The assembly code builder</param>
    /// <param name="node">The expression to compile</param>
    /// <returns>Value representing the result of the expression</returns>
    private Value CompileValue(StringBuilder builder, SyntaxNode node)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                if (!variables.TryGetValue(
                    GetAssemblySymbol(identifier.Value), out CompiledVariable variable
                )) throw new UnknownIdentifierException(identifier);

                return new SymbolValue(variable.Type, variable.AssemblySymbol);

            case IntegerNode integer:
                return new IntegerValue(COMPILED_TYPES["i32"], integer.Value);

            default:
                throw new UnexpectedSyntaxNodeException(node, "expression");
        }
    }

    /// <summary>
    /// Compiles a statement and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="builder">The assembly code builder</param>
    /// <param name="node">The statement to compile</param>
    private void CompileStatement(StringBuilder builder, SyntaxNode node)
    {
        switch (node)
        {
            case AssignmentNode assignment:
                Value left = CompileValue(builder, assignment.Left);
                Value right = CompileValue(builder, assignment.Right);

                if (!left.IsLValue)
                    throw new NotLValueException(assignment.Left);

                builder.Append($"\tmov\t{left}, {right}\n");
                break;

            default:
                throw new UnexpectedSyntaxNodeException(node, "statement");
        }
    }

    /// <summary>
    /// Compiles a function and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="builder">The assembly code builder</param>
    /// <param name="function">The function to compile</param>
    private void CompileFunction(StringBuilder builder, CompiledFunction function)
    {
        foreach (SyntaxNode node in function.Body)
        {
            CompileStatement(builder, node);
        }
    }

    /// <summary>
    /// Linkes all the compiled nodes and returns the generated FASM code
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
use32


section '.text' readable executable

"
        );

        foreach (var pair in functions)
        {
            builder.AppendFormat("{0}:\n", pair.Key);
            CompileFunction(builder, pair.Value);
        }

        builder.Append("\n\nsection '.data' readable writable\n\n");

        foreach (CompiledVariable variable in variables.Values)
        {
            builder.AppendFormat(
                "{0}:\n\t{1}\t{2}\n",
                variable.AssemblySymbol,
                variable.Type.Declaration,
                variable.AssemblyValue
            );
        }

        return builder.ToString();
    }
}