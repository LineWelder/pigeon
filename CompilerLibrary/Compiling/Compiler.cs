using System;
using System.Collections.Generic;
using CompilerLibrary.Compiling.Exceptions;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling;

/// <summary>
/// Generates FASM code based on parsed Pigeon
/// </summary>
public class Compiler
{
    internal static readonly Dictionary<string, CompiledType> COMPILED_TYPES = new()
    {
        { "i32", new CompiledType(Size: 4, Declaration: "dd", Name: "dword", Abbreviation: 'i', IsSigned: true) },
        { "i16", new CompiledType(Size: 2, Declaration: "dw", Name: "word",  Abbreviation: 's', IsSigned: true) },
        { "i8",  new CompiledType(Size: 1, Declaration: "db", Name: "byte",  Abbreviation: 'c', IsSigned: true) },
    };

    private readonly Dictionary<string, CompiledVariable> variables = new();
    private readonly Dictionary<string, CompiledFunction> functions = new();

    private AssemblyGenerator assemblyGenerator;
    private RegisterManager registerManager;

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

        string assemblySymbol = AssemblyGenerator.GetAssemblySymbol(function);

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
                "possible value loss"
            );

        string assemblySymbol = AssemblyGenerator.GetAssemblySymbol(variable.Identifier);
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
    public void RegisterNodes(SyntaxNode[] nodes)
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
    /// Converts an IntegerValue into another type
    /// </summary>
    /// <param name="node">The syntax node the conversion happens within,
    /// used for exception throwing</param>
    /// <param name="value">The value to convert</param>
    /// <param name="type">The type to convert into</param>
    /// <returns>The converted integer value</returns>
    private static IntegerValue ConvertIntegerValue(SyntaxNode node, IntegerValue value, CompiledType type)
    {
        if (value.Type.IsSigned != type.IsSigned && value.Value < 0)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type.Name, type.Name,
                "cannot change type's signedness"
            );
        }

        if (value.Value > type.MaximumValue || value.Value < type.MinimumValue)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type.Name, type.Name,
                "possible value loss"
            );
        }

        return new IntegerValue(type, value.Value);
    }

    /// <summary>
    /// Generates data transfer from the source to the destination
    /// </summary>
    /// <param name="node">The syntax node the transfer happens within,
    /// used for exception throwing</param>
    /// <param name="destination">The location to put data to</param>
    /// <param name="source">The location to take data from</param>
    private void GenerateMov(SyntaxNode node, Value left, Value right)
    {
        // We cannot transfer data from a variable to another directly
        if (right is SymbolValue && left is not RegisterValue)
        {
            RegisterValue transferRegister = registerManager.AllocateRegister(node, right.Type);
            assemblyGenerator.EmitInstruction("mov", transferRegister.ToString(), right.ToString());
            right = transferRegister;
        }

        if (right is IntegerValue integerValue)
        {
            assemblyGenerator.EmitInstruction(
                "mov", left.ToString(), ConvertIntegerValue(node, integerValue, left.Type).ToString()
            );
        }
        else if (left.Type.Size == right.Type.Size)
        {
            assemblyGenerator.EmitInstruction("mov", left.ToString(), right.ToString());
        }
        else if (left.Type.Size > right.Type.Size)
        {
            if (left.Type.IsSigned != right.Type.IsSigned)
            {
                throw new InvalidTypeCastException(
                    node.Location, right.Type.Name, left.Type.Name,
                    "the types must be either both signed or unsigned"
                );
            }

            assemblyGenerator.EmitInstruction(
                left.Type.IsSigned ? "movsx" : "movzx",
                left.ToString(), right.ToString()
            );
        }
        else
        {
            throw new InvalidTypeCastException(
                node.Location, right.Type.Name, left.Type.Name,
                "possible value loss"
            );
        }
    }

    /// <summary>
    /// Generates conversion of the value to the type
    /// </summary>
    /// <param name="node">The syntax node the conversion happens within,
    /// used for exception throwing</param>
    /// <param name="value">The value to convert</param>
    /// <param name="type">The type to convert to</param>
    /// <returns>The location of the converted value</returns>
    private Value GenerateTypeCast(SyntaxNode node, Value value, CompiledType type)
    {
        if (value.Type == type)
        {
            return value;
        }

        if (value is IntegerValue integerValue)
        {
            return ConvertIntegerValue(node, integerValue, type);
        }

        if (value.Type.IsSigned != type.IsSigned)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type.Name, type.Name,
                "cannot change type's signedness"
            );
        }

        if (value.Type.Size > type.Size)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type.Name, type.Name,
                "possible value loss"
            );
        }

        switch (value)
        {
            case RegisterValue { Name: string registerName }:
                int registerId = RegisterManager.GetRegisterIdFromName(registerName);
                string convetedRegister = RegisterManager.GetRegisterNameFromId(registerId, type);

                assemblyGenerator.EmitInstruction(
                    "and", convetedRegister, (Math.Pow(2, type.Size) - 1).ToString()
                );

                return new RegisterValue(type, convetedRegister);

            default:
                RegisterValue conversionRegister = registerManager.AllocateRegister(node, type);
                assemblyGenerator.EmitInstruction(
                    type.IsSigned ? "movsx" : "movzx",
                    conversionRegister.ToString(), value.ToString()
                );

                return conversionRegister;
        }
    }

    /// <summary>
    /// Compiels an expression and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="node">The expression to compile</param>
    /// <returns>Value representing the result of the expression</returns>
    private Value CompileValue(SyntaxNode node, CompiledType targetType = null)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                if (!variables.TryGetValue(
                    AssemblyGenerator.GetAssemblySymbol(identifier.Value),
                    out CompiledVariable variable
                )) throw new UnknownIdentifierException(identifier);

                return new SymbolValue(variable.Type, variable.AssemblySymbol);

            case IntegerNode integer:
                CompiledType type = COMPILED_TYPES["i32"];
                if (integer.Value > type.MaximumValue || integer.Value < type.MinimumValue)
                    throw new InvalidTypeCastException(
                        integer.Location,
                        "bigger integer size", type.Name,
                        "possible value loss"
                    );

                return new IntegerValue(type, integer.Value);

            case BinaryNode binary:
                Value left = CompileValue(binary.Left, targetType);
                Value right = CompileValue(binary.Right, targetType);

                if (left.Type.IsSigned != right.Type.IsSigned)
                {
                    throw new InvalidTypeCastException(
                        binary.Location,
                        right.Type.Name, left.Type.Name,
                        "operand types must be either both signed or unsigned"
                    );
                }

                CompiledType resultType = targetType
                    ?? (left.Type.Size > right.Type.Size
                        ? left.Type : right.Type);

                if (left is not RegisterValue)
                {
                    if (right is RegisterValue && binary.Operation is BinaryNodeOperation.Addition)
                    {
                        (left, right) = (right, left);
                    }
                    else
                    {
                        RegisterValue accumulator = registerManager.AllocateRegister(binary, resultType);
                        GenerateMov(binary, accumulator, left);
                        left = accumulator;
                    }
                }

                right = GenerateTypeCast(binary, right, resultType);

                assemblyGenerator.EmitInstruction(
                    binary.Operation switch
                    {
                        BinaryNodeOperation.Addition    => "add",
                        BinaryNodeOperation.Subtraction => "sub",
                        _ => throw new System.NotImplementedException()
                    },
                    left.ToString(), right.ToString()
                );

                registerManager.FreeRegister(right);
                return left;

            default:
                throw new UnexpectedSyntaxNodeException(node, "expression");
        }
    }

    /// <summary>
    /// Compiles a statement and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="node">The statement to compile</param>
    private void CompileStatement(SyntaxNode node)
    {
        switch (node)
        {
            case AssignmentNode assignment:
                Value left = CompileValue(assignment.Left);
                Value right = CompileValue(assignment.Right, left.Type);

                if (left is not SymbolValue)
                    throw new NotLValueException(assignment.Left);

                GenerateMov(assignment, left, right);

                registerManager.FreeRegister(right);
                break;

            default:
                throw new UnexpectedSyntaxNodeException(node, "statement");
        }
    }

    /// <summary>
    /// Compiles a function and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="function">The function to compile</param>
    private void CompileFunction(CompiledFunction function)
    {
        foreach (SyntaxNode node in function.Body)
        {
            CompileStatement(node);
        }
    }

    /// <summary>
    /// Linkes all the compiled nodes and returns the generated FASM code
    /// </summary>
    /// <returns>The generated FASM code</returns>
    public string CompileAll()
    {
        assemblyGenerator = new AssemblyGenerator();
        registerManager = new RegisterManager();

        foreach (var pair in variables)
        {
            assemblyGenerator.EmitVariable(
                pair.Key, pair.Value.Type.Declaration,
                pair.Value.AssemblyValue
            );
        }

        foreach (var pair in functions)
        {
            assemblyGenerator.EmitSymbol(pair.Key);
            CompileFunction(pair.Value);
        }

        return assemblyGenerator.LinkAssembly();
    }
}