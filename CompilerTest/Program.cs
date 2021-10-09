using System;
using System.IO;
using System.Text;
using CompilerLibrary.Tokenizing;

namespace CompilerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = "i32 test \n= hello;";

            byte[] byteArray = Encoding.ASCII.GetBytes(code);
            MemoryStream stream = new(byteArray);
            Tokenizer tokenizer = new("<string>", new StreamReader(stream));

            do Console.WriteLine(tokenizer.NextToken());
            while (!tokenizer.ReachedTheEOF);
        }
    }
}
