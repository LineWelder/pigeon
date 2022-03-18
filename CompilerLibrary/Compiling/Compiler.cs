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
        { "i32", new TypeInfo(Size: 4, Name: "i32", IsSigned: true) },
        { "i16", new TypeInfo(Size: 2, Name: "i16", IsSigned: true) },
        { "i8",  new TypeInfo(Size: 1, Name: "i8",  IsSigned: true) },
        { "u32", new TypeInfo(Size: 4, Name: "u32", IsSigned: false) },
        { "u16", new TypeInfo(Size: 2, Name: "u16", IsSigned: false) },
        { "u8",  new TypeInfo(Size: 1, Name: "u8",  IsSigned: false) }
    };

    private readonly Dictionary<string, VariableInfo> variables = new()
    {
        { "_input", new(SourceLocation: new("", 0, 0), AssemblySymbol: "_input",
            Type: COMPILED_TYPES["i32"], AssemblyValue: "0") }
    };
    private readonly Dictionary<string, FunctionInfo> functions = new();

    private FunctionInfo currentFunction;
    private bool needsEndingLabel;

    private AssemblyGenerator assemblyGenerator;
    private RegisterManager registerManager;

    public Compiler() { }

    /// <summary>
    /// Returns the TypeInfo for the given type
    /// </summary>
    public static TypeInfo GetTypeInfo(SyntaxNode type)
    {
        if (type is not IdentifierNode identifier)
        {
            throw new UnexpectedSyntaxNodeException(type, "type identifier");
        }

        if (COMPILED_TYPES.TryGetValue(identifier.Value, out TypeInfo compiledType))
        {
            return compiledType;
        }

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

        string assemblySymbol = AssemblyGenerator.GetAssemblySymbol(function.Identifier);

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
        {
            throw new UnexpectedSyntaxNodeException(variable.Value, "a number");
        }

        long maximumValue = variableType.MaximumValue;
        if (valueInteger.Value > maximumValue)
        {
            throw new InvalidTypeCastException(
                variable.Value.Location,
                null, variableType,
                "possible value loss"
            );
        }

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
        if (value.Type is not null
         && value.Type.IsSigned != type.IsSigned
         && value.Value < 0)
        {
            throw new InvalidTypeCastException(
                node.Location,
                value.Type, type,
                "cannot change type's signedness"
            );
        }

        if (value.Value > type.MaximumValue || value.Value < type.MinimumValue)
        {
            if (!explicitly)
            {
                throw new InvalidTypeCastException(
                    node.Location,
                    value?.Type, type,
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
    /// <param name="explicitTypeCast">If true, allows unsafe type casts</param>
    private void GenerateMov(
        SyntaxNode node, Value destination,
        Value source, bool explicitTypeCast = false)
    {
        if (!explicitTypeCast && destination.Type.IsSigned != source.Type.IsSigned)
        {
            throw new InvalidTypeCastException(
                node.Location,
                source.Type, destination.Type,
                "the types must be either both signed or unsigned"
            );
        }

        if (!explicitTypeCast && destination.Type.Size < source.Type.Size)
        {
            throw new InvalidTypeCastException(
                node.Location, source.Type, destination.Type,
                "possible value loss"
            );
        }

        if (destination == source)
        {
            return;
        }

        // We cannot transfer data from a variable to another directly
        if (source is SymbolValue && destination is not RegisterValue)
        {
            RegisterValue transferRegister = registerManager.AllocateRegister(node, source.Type);
            assemblyGenerator.EmitInstruction("mov", transferRegister, source);
            source = transferRegister;
        }

        if (source is IntegerValue integerValue)
        {
            assemblyGenerator.EmitInstruction(
                "mov", destination,
                ConvertIntegerValue(
                    node, integerValue, destination.Type, explicitTypeCast
                )
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
            Value converted;
            switch (source)
            {
                case RegisterValue register:
                    converted = register with { Type = destination.Type };
                    break;

                case SymbolValue symbol:
                    converted = symbol with { Type = destination.Type };
                    break;

                case IntegerValue integer:
                    converted = ConvertIntegerValue(node, integer, destination.Type, true);
                    break;

                default:
                    throw new ArgumentException("Unexpected value class", nameof(source));
            }

            if (destination == converted)
            {
                return;
            }

            assemblyGenerator.EmitInstruction("mov", destination, converted);
        }

        registerManager.FreeRegister(source);
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
            int registerId = registerManager.GetRegisterIdFromAllocation(register);
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

            return register with { Type = type };
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
                value.Type, type,
                "cannot change type's signedness"
            );
        }

        if (value.Type.Size > type.Size)
        {
            if (!explicitly)
            {
                throw new InvalidTypeCastException(
                    node.Location,
                    value.Type, type,
                    "possible value loss"
                );
            }

            return value switch
            {
                RegisterValue registerValue => CutRegister(registerValue, type),
                SymbolValue => value with { Type = type },
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
    /// Finds the variable of the given identifier
    /// </summary>
    /// <param name="identifier">The identifier of the variable to find</param>
    /// <returns>SymbolValue representing the found variable</returns>
    private SymbolValue FindSymbol(IdentifierNode identifier)
    {
        string symbol = AssemblyGenerator.GetAssemblySymbol(identifier.Value);
        if (variables.TryGetValue(symbol, out VariableInfo variable))
        {
            return new SymbolValue(variable.Type, variable.AssemblySymbol);
        }
        else if (functions.TryGetValue(symbol, out FunctionInfo function))
        {
            return new SymbolValue(
                new FunctionPointerTypeInfo(function),
                function.AssemblySymbol
            );
        }
        else
        {
            throw new UnknownIdentifierException(identifier);
        }
    }

    /// <summary>
    /// Evaluates the type of an expression
    /// </summary>
    /// <param name="node">The expression to evaluate type of</param>
    /// <returns>The evaluated type, if cannot be evaluated - null</returns>
    public TypeInfo? EvaluateType(SyntaxNode node)
    {
        switch (node)
        {
            case IdentifierNode identifier:
                return FindSymbol(identifier).Type;

            case IntegerNode:
                return null;

            case TypeCastNode typeCast:
                return GetTypeInfo(typeCast.Type);

            case NegationNode negation:
                TypeInfo? innerType = EvaluateType(negation.InnerExpression);
                if (!(innerType?.IsSigned ?? true))
                {
#warning Create UnsignedTypeException
                    throw new InvalidTypeCastException(
                        negation.Location,
                        innerType, innerType,
                        "cannot apply negation to an unsigned type"
                    );
                }

                return innerType;

            case FunctionCallNode functionCall:
                TypeInfo function = EvaluateType(functionCall.Function);
                if (function is not FunctionPointerTypeInfo functionType)
                {
#warning Create UncallableTypeException
                    throw new InvalidTypeCastException(
                        functionCall.Location,
                        function, function,
                        "uncallable type"
                    );
                }

                return functionType.FunctionInfo.ReturnType;

            case BinaryNode binary:
                TypeInfo? leftType = EvaluateType(binary.Left);
                TypeInfo? rightType = EvaluateType(binary.Right);

                if (leftType is not null && rightType is not null
                 && leftType.IsSigned != rightType.IsSigned)
                {
                    throw new InvalidTypeCastException(
                        binary.Location,
                        rightType, leftType,
                        "operand types must be either both signed or unsigned"
                    );
                }

                return (leftType?.Size ?? 0) > (rightType?.Size ?? 0)
                    ? leftType : rightType;

            default:
                throw new UnexpectedSyntaxNodeException(node, "expression");
        }
    }

    /// <summary>
    /// Generates a function call
    /// </summary>
    /// <param name="node">The node the call happens within</param>
    /// <param name="function">The function to call</param>
    /// <param name="mustReturnValue">Whether the function must return a value</param>
    /// <returns>The location of the returned value</returns>
    private Value? GenerateFunctionCall(SyntaxNode node, Value function, bool mustReturnValue)
    {
        if (function.Type is not FunctionPointerTypeInfo functionType)
        {
#warning Create UncallableTypeException
            throw new InvalidTypeCastException(
                node.Location,
                function.Type, function.Type,
                "uncallable type"
            );
        }

        RegisterValue? returnRegister = null;
        if (mustReturnValue)
        {
            if (functionType.FunctionInfo.ReturnType is null)
            {
                throw new NoReturnValueException(
                    node.Location,
                    function.Type.Name
                );
            }

            // The register the function result is returned in
            (returnRegister, int oldValueNewRegister) = registerManager.RequireRegister(
                node, functionType.FunctionInfo.ReturnType,
                RegisterManager.RETURN_REGISTER_ID
            );

            if (oldValueNewRegister >= 0)
            {
                assemblyGenerator.EmitInstruction(
                    "mov",
                    RegisterManager.GetRegisterNameFromId(
                        oldValueNewRegister, COMPILED_TYPES["i32"]),
                    RegisterManager.GetRegisterNameFromId(
                        RegisterManager.RETURN_REGISTER_ID, COMPILED_TYPES["i32"])
                );
            }
        }

        assemblyGenerator.EmitInstruction("call", function);
        return returnRegister;
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
                return FindSymbol(identifier);

            case IntegerNode integer:
                if (targetType is not null
                 && (integer.Value > targetType.MaximumValue
                  || integer.Value < targetType.MinimumValue))
                {
                    throw new InvalidTypeCastException(
                        integer.Location,
                        null, targetType,
                        "possible value loss"
                    );
                }

                return new IntegerValue(targetType, integer.Value);

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
#warning Create UnsignedTypeException
                    throw new InvalidTypeCastException(
                        negation.Location,
                        inner.Type, inner.Type,
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

            case FunctionCallNode functionCall:
                Value function = CompileValue(functionCall.Function);
                return GenerateFunctionCall(functionCall, function, true);

            case BinaryNode binary:
                TypeInfo resultType = EvaluateType(binary) ?? targetType;

                Value left = CompileValue(binary.Left, resultType);
                Value right = CompileValue(binary.Right, resultType);

                if (left.Type.IsSigned != right.Type.IsSigned)
                {
                    throw new InvalidTypeCastException(
                        binary.Location,
                        right.Type, left.Type,
                        "operand types must be either both signed or unsigned"
                    );
                }

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
    /// Compiles the expression, and movs it's result into destination
    /// </summary>
    /// <param name="node">The node of the assignment</param>
    /// <param name="destination">The location to mov the value into</param>
    /// <param name="expression">The expression to get value from</param>
    private void GenerateAssignment(SyntaxNode node, Value destination, SyntaxNode expression)
    {
        expression = Optimizer.OptimizeExpression(expression);
        Value value;

        if (expression is TypeCastNode typeCast)
        {
            TypeInfo typeInfo = GetTypeInfo(typeCast.Type);
            if (typeInfo == destination.Type)
            {
                value = CompileValue(typeCast.Value);
                GenerateMov(
                    node,
                    destination, value,
                    true
                );

                goto endMov;
            }
        }

        value = CompileValue(expression, destination.Type);
        GenerateMov(node, destination, value);

    endMov:
        registerManager.FreeRegister(value);
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
                if (left is not SymbolValue)
                {
                    throw new NotLValueException(assignment.Left);
                }

                GenerateAssignment(assignment, left, assignment.Right);
                break;

            case ReturnNode @return:
                if (@return.InnerExpression is null != currentFunction.ReturnType is null)
                {
                    throw new MismatchingReturnException(
                        @return.Location, currentFunction.ReturnType
                    );
                }

                if (@return.InnerExpression is not null)
                {
                    RegisterValue returnRegister
                        = registerManager.GetReturnRegister(currentFunction.ReturnType);

                    GenerateAssignment(@return, returnRegister, @return.InnerExpression);
                    registerManager.FreeRegister(returnRegister);
                }

                if (@return != currentFunction.Body[^1])
                {
                    assemblyGenerator.EmitInstruction("jmp", $"end{currentFunction.AssemblySymbol}");
                    needsEndingLabel = true;
                }
                break;

            case FunctionCallNode functionCall:
                Value function = CompileValue(functionCall.Function);
                GenerateFunctionCall(functionCall, function, false);
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
            currentFunction = pair.Value;
            needsEndingLabel = false;

            foreach (SyntaxNode node in pair.Value.Body)
            {
                CompileStatement(node);
            }

            // Beginning
            assemblyGenerator.EmitSymbol(pair.Key);
            assemblyGenerator.EmitInstructionToText("push", "ebp");
            assemblyGenerator.EmitInstructionToText("mov", "ebp", "esp");
            foreach (string register in registerManager.Used)
            {
                assemblyGenerator.EmitInstructionToText("push", register);
            }

            // Code
            assemblyGenerator.InsertFunctionCode();

            // Ending
            if (needsEndingLabel)
            {
                assemblyGenerator.EmitSymbol($"end{currentFunction.AssemblySymbol}");
            }
            foreach (string register in registerManager.Used)
            {
                assemblyGenerator.EmitInstructionToText("pop", register);
            }
            assemblyGenerator.EmitInstructionToText("leave");
            assemblyGenerator.EmitInstructionToText("ret");

            registerManager.ResetUsedRegisters();
        }

        return assemblyGenerator.LinkAssembly();
    }
}