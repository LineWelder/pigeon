using System;
using System.IO;
using System.Text;
using CompilerLibrary;
using CompilerLibrary.Parsing;
using CompilerLibrary.Tokenizing;

namespace CompilerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = @"
i32 test = 2;
i32 test2 = test;";

            byte[] byteArray = Encoding.ASCII.GetBytes(code);
            MemoryStream stream = new(byteArray);
            Tokenizer tokenizer = new("<string>", new StreamReader(stream));
            Parser parser = new(tokenizer);

            try
            {
                Console.WriteLine(parser.Parse());
                Console.WriteLine(parser.Parse());
            }
            catch (CompilerException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
