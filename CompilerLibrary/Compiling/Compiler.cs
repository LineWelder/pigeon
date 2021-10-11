using System.Collections.Generic;
using System.Text;
using CompilerLibrary.Compiling.Exceptions;
using CompilerLibrary.Parsing;

namespace CompilerLibrary.Compiling
{
    /// <summary>
    /// Generates FASM code based on parsed Pigeon
    /// </summary>
    public class Compiler
    {
        private static readonly Dictionary<string, CompiledType> COMPILED_TYPES = new()
        {
            { "i32", new CompiledType(4, "dd", 'i') },
            { "void", new CompiledType(0, "", 'v') }
        };

        public readonly Dictionary<string, CompiledFunction> functions = new();

        public Compiler()
        {

        }

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
        /// Returns the assembly symbol for function declaration
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblySymbol(FunctionDeclarationNode function)
        {
            StringBuilder builder = new();

            builder.AppendFormat("_{0}", function.Identifier);
            if (function.ArgumentList.Length > 0)
                builder.Append('@');
            foreach (FunctionArgumentDeclarationNode argument in function.ArgumentList)
                builder.Append(GetCompiledType(argument.Type));

            return builder.ToString();
        }

        /// <summary>
        /// Compiles a function
        /// </summary>
        /// <param name="function">The function to compile</param>
        public void CompileFunction(FunctionDeclarationNode function)
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
                GetCompiledType(function.ReturnType),
                arguments,
                "\tnop\n"
            ));
        }

        /// <summary>
        /// Linkes all the compiled nodes and returns the generated FASM code
        /// </summary>
        /// <returns>The generated FASM code</returns>
        public string LinkAssembly()
        {
            StringBuilder builder = new(
@"format PE console
use32

section '.text' readable executable

"
            );

            foreach (var pair in functions)
            {
                builder.AppendFormat("{0}:\n", pair.Key);
                builder.Append(pair.Value.AssemblyCode);
            }

            return builder.ToString();
        }
    }
}
