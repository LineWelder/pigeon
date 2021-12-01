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
    internal static readonly Dictionary<string, TypeInfo> COMPILED_TYPES = new()
    {
        { "i32", new TypeInfo(Size: 4, Name: "i32", Abbreviation: 'i', IsSigned: true) },
        { "i16", new TypeInfo(Size: 2, Name: "i16", Abbreviation: 's', IsSigned: true) },
        { "i8",  new TypeInfo(Size: 1, Name: "i8",  Abbreviation: 'c', IsSigned: true) },
        { "u32", new TypeInfo(Size: 4, Name: "u32", Abbreviation: 'd', IsSigned: false) },
        { "u16", new TypeInfo(Size: 2, Name: "u16", Abbreviation: 'w', IsSigned: false) },
        { "u8",  new TypeInfo(Size: 1, Name: "u8",  Abbreviation: 'b', IsSigned: false) }
    };

    private readonly Dictionary<string, VariableInfo> variables = new();
    private readonly Dictionary<string, FunctionInfo> functions = new();

    private AssemblyGenerator assemblyGenerator;
    private RegisterManager registerManager;

    public Compiler() { }

    /// <summary>
    /// Returns the TypeInfo for the given type
    /// </summary>
    public static TypeInfo GetTypeInfo(SyntaxNode type)
    {
        if (type is not IdentifierNode identifier)
            throw new UnexpectedSyntaxNodeException(type, "type identifier");

        if (COMPILED_TYPES.TryGetValue(identifier.Value, out TypeInfo compiledType))
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
                GetTypeInfo(argument.Type),
                argument.Identifier
            );
        }

        string assemblySymbol = AssemblyGenerator.GetAssemblySymbol(function);

        functions.Add(assemblySymbol, new FunctionInfo(
            function.Location,
            assemblySymbol,
            function.ReturnType is null ? null : GetTypeInfo(function.ReturnType),
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
        TypeInfo variableType = GetTypeInfo(variable.Type);
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

        variables.Add(assemblySymbol, new VariableInfo(
            variable.Location,
            assemblySymbol,
            variableType,
            variableValue
        ));
    }

    /// <summary>
    /// Compiles the given declarations
    /// </summary>
    public void RegisterDeclarations(SyntaxNode[] nodes)
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
    /// <param name="explicitly"></param>
    /// <returns>The converted integer value</returns>
    private static IntegerValue ConvertIntegerValue(
        SyntaxNode node, IntegerValue value, TypeInfo type, bool explicitly = false
    )
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
            if (!explicitly)
            {
                throw new InvalidTypeCastException(
                    node.Location,
                    value.Type.Name, type.Name,
                    "possible value loss"
                );
            }

            long cutted = value.Value & type.Mask;
            long maxVal = type.MaximumValue;
            if (type.IsSigned && cutted > maxVal)
            {
                cutted = cutted - 2 * maxVal - 2;
            }

            return new IntegerValue(type, cutted);
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
    private void GenerateMov(SyntaxNode node, Value destination, Value source)
    {
        // We cannot transfer data from a variable to another directly
        if (source is SymbolValue && destination is not RegisterValue)
        {
            RegisterValue transferRegister = registerManager.AllocateRegister(node, source.Type);
            assemblyGenerator.EmitInstruction("mov", transferRegister, source);
            source = transferRegister;
        }

        if (destination.Type.IsSigned != source.Type.IsSigned)
        {
            throw new InvalidTypeCastException(
                node.Location,
                source.Type.Name, destination.Type.Name,
                "the types must be either both signed or unsigned"
            );
        }

        if (source is IntegerValue integerValue)
        {
            assemblyGenerator.EmitInstruction(
                "mov", destination, ConvertIntegerValue(node, integerValue, destination.Type)
            );
        }
        else if (destination.Type.Size == source.Type.Size)
        {
            assemblyGenerator.EmitInstruction("mov", destination, source);
        }
        else if (destination.Type.Size > source.Type.Size)
        {
            assemblyGenerator.EmitInstruction(
                destination.Type.IsSigned ? "movsx" : "movzx",
                destination, source
            );
        }
        else
        {
            throw new InvalidTypeCastException(
                node.Location, source.Type.Name, destination.Type.Name,
                "possible value loss"
            );
        }

        registerManager.FreeRegister(source);
    }

    private void GenerateAssignmentWithExplicitTypeCast(
        SyntaxNode node, Value value, Value destination, Value source)
    {
        if (value.Type.Size > destination.Type.Size)
        {
            switch (value)
            {
                case RegisterValue register:
                    int registerId = RegisterManager.GetRegisterIdFromName(register.Name);
                    string convertedRegister = RegisterManager.GetRegisterNameFromId(
                        registerId, destination.Type
                    );

                    assemblyGenerator.EmitInstruction(
                        "mov", destination, convertedRegister
                    );
                    break;

                case SymbolValue :
                    int registerId = RegisterManager.GetRegisterIdFromName(register.Name);
                    string convertedRegister = RegisterManager.GetRegisterNameFromId(
                        registerId, destination.Type
                    );

                    assemblyGenerator.EmitInstruction(
                        "mov", destination, convertedRegister
                    );
                    break;
            }
        }

        GenerateMov(node, destination, value);
    }

    /// <summary>
    /// Generates conversion of the value to the type
    /// </summary>
    /// <param name="node">The syntax node the conversion happens within,
    /// used for exception throwing</param>
    /// <param name="value">The value to convert</param>
    /// <param name="type">The type to convert to</param>
    /// <param name="explicitly">Whether the type cast is explicit</param>
    /// <returns>The location of the converted value</returns>
    private Value GenerateTypeCast(
        SyntaxNode node, Value value,
        TypeInfo type, bool explicitly = false)
    {
        RegisterValue CutRegister(RegisterValue register, TypeInfo type)
        {
            int registerId = RegisterManager.GetRegisterIdFromName(register.Name);
            string convertedRegister = RegisterManager.GetRegisterNameFromId(registerId, type);

            if (type.Size > register.Type.Size)
            {
                if (type.IsSigned)
                {
                    assemblyGenerator.EmitInstruction(
                        "movsx", convertedRegister, register
                    );
                }
                else
                {
                    assemblyGenerator.EmitInstruction(
                        "and", convertedRegister, register.Type.Mask
                    );
                }
            }

            return new RegisterValue(type, convertedRegister);
        }

        if (value.Type == type)
        {
            return value;
        }

        if (value is IntegerValue integerValue)
        {
            return ConvertIntegerValue(node, integerValue, type, explicitly);
        }

        if (value.Type.IsSigned != type.IsSigned && !explicitly)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type.Name, type.Name,
                "cannot change type's signedness"
            );
        }

        if (value.Type.Size > type.Size)
        {
            if (!explicitly)
            {
                throw new InvalidTypeCastException(
                    node.Location,
                    value.Type.Name, type.Name,
                    "possible value loss"
                );
            }

            return value switch
            {
                RegisterValue registerValue => CutRegister(registerValue, type),
                SymbolValue                 => value with { Type = type },
                _ => throw new ArgumentException("Unexpected value type"),
            };
        }

        switch (value)
        {
            case RegisterValue registerValue:
                return CutRegister(registerValue, type);

            default:
                RegisterValue conversionRegister = registerManager.AllocateRegister(node, type);
                assemblyGenerator.EmitInstruction(
                    type.IsSigned ? "movsx" : "movzx",
                    conversionRegister, value
                );

                return conversionRegister;
        }
    }

    /// <summary>
    /// Compiels an expression and appends the compiled assembly to the builder
    /// </summary>
    /// <param name="node">The expression to compile</param>
    /// <returns>Value representing the result of the expression</returns>
    private Value CompileValue(SyntaxNode node, TypeInfo targetType = null)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                if (!variables.TryGetValue(
                    AssemblyGenerator.GetAssemblySymbol(identifier.Value),
                    out VariableInfo variable
                )) throw new UnknownIdentifierException(identifier);

                return new SymbolValue(variable.Type, variable.AssemblySymbol);

            case IntegerNode integer:
                TypeInfo type = targetType;
                if (integer.Value > type.MaximumValue || integer.Value < type.MinimumValue)
                    throw new InvalidTypeCastException(
                        integer.Location,
                        "bigger integer size", type.Name,
                        "possible value loss"
                    );

                return new IntegerValue(type, integer.Value);

            case TypeCastNode typeCast:
                TypeInfo castInto = GetTypeInfo(typeCast.Type);
                return GenerateTypeCast(
                    typeCast,
                    CompileValue(typeCast.Value, castInto),
                    castInto, true
                );

            case NegationNode negation:
                Value inner = CompileValue(negation.InnerExpression, targetType);

                if (!inner.Type.IsSigned)
                {
                    throw new InvalidTypeCastException(
                        negation.Location,
                        inner.Type.Name, "a signed type",
                        "cannot apply negation to an unsigned type"
                    );
                }

                if (inner is not RegisterValue)
                {
                    RegisterValue resultRegister = registerManager.AllocateRegister(negation, inner.Type);
                    GenerateMov(negation, resultRegister, inner);
                    inner = resultRegister;
                }

                assemblyGenerator.EmitInstruction("neg", inner);
                return inner;

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

                TypeInfo resultType = targetType
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
                        _ => throw new NotImplementedException()
                    },
                    left, right
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
                Value right = CompileValue(Optimizer.OptimizeExpression(assignment.Right), left.Type);

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
                pair.Key, pair.Value.Type.AssemblyDeclaration,
                pair.Value.AssemblyValue
            );
        }

        foreach (var pair in functions)
        {
            assemblyGenerator.EmitSymbol(pair.Key);
            foreach (SyntaxNode node in pair.Value.Body)
            {
                CompileStatement(node);
            }
        }

        return assemblyGenerator.LinkAssembly();
    }
}