using System;
using System.IO;
using System.Text;
using CompilerLibrary;
using CompilerLibrary.Compiling;
using CompilerLibrary.Parsing;
using CompilerLibrary.Tokenizing;

// ====================================
//  TODO:
// > Explicit type casts
// * Function stack frames and register saving
// * Return statements
// * Function arguments
// * Global initializations, _start and i32 main()
// * Implement multiplication and division
// * Function pointers
// * Merge CompiledVariable, CompiledFunction and SymbolValue
// ====================================

namespace CompilerTest;

class Program
{
    static void Main(string[] args)
    {
        string code = @"
i32 dword = 30;
i16 word  = 29;
i8  byte  = 8;

main()
{
    word = dword:i32;
}
";

        byte[] byteArray = Encoding.ASCII.GetBytes(code);
        MemoryStream stream = new(byteArray);
        Tokenizer tokenizer = new("<string>", new StreamReader(stream));
        Parser parser = new(tokenizer);
        Compiler compiler = new();

        try
        {
            SyntaxNode[] nodes = parser.ParseFile();
            compiler.RegisterNodes(nodes);
            Console.WriteLine(compiler.CompileAll());
        }
        catch (CompilerException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
