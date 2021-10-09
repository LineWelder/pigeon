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
            string code = "test\nhello";

            byte[] byteArray = Encoding.ASCII.GetBytes(code);
            MemoryStream stream = new(byteArray);
            Tokenizer tokenizer = new(new StreamReader(stream));

            Console.WriteLine(tokenizer.NextToken());
            Console.WriteLine(tokenizer.NextToken());
            Console.WriteLine(tokenizer.NextToken());
        }
    }
}
