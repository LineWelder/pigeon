using System;
using System.IO;
using System.Text;
using CompilerLibrary;
using CompilerLibrary.Compiling;
using CompilerLibrary.Parsing;
using CompilerLibrary.Tokenizing;

// ====================================
//  TODO:
// * Make pretty ToString method for syntax nodes
// * Make UnexpectedSyntaxNodeException write the unexpected node
// ====================================

namespace CompilerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = @"
i32 test = 43;
fix()
{
    test = 29;
}";

            byte[] byteArray = Encoding.ASCII.GetBytes(code);
            MemoryStream stream = new(byteArray);
            Tokenizer tokenizer = new("<string>", new StreamReader(stream));
            Parser parser = new(tokenizer);
            Compiler compiler = new();

            try
            {
                SyntaxNode[] nodes = parser.ParseFile();
                compiler.CompileNodes(nodes);
                Console.WriteLine(compiler.LinkAssembly());
            }
            catch (CompilerException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
