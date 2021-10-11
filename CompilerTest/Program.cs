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
void main()
{
    a = 1;
}";

            byte[] byteArray = Encoding.ASCII.GetBytes(code);
            MemoryStream stream = new(byteArray);
            Tokenizer tokenizer = new("<string>", new StreamReader(stream));
            Parser parser = new(tokenizer);
            Compiler compiler = new Compiler();

            try
            {
                FunctionDeclarationNode funciton = (FunctionDeclarationNode)parser.Parse();
                // Debug.PrintSyntaxNode(funciton);
                compiler.CompileFunction(funciton);

                Console.WriteLine(compiler.LinkAssembly());
            }
            catch (CompilerException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
