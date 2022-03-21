using System;
using System.IO;
using System.Text;
using CompilerLibrary;
using CompilerLibrary.Compiling;
using CompilerLibrary.Parsing;
using CompilerLibrary.Tokenizing;

#warning TODO Add function arguments to readme.md
#warning TODO Revise GenerateAssignment value freeing

const string code = @"
i32 test = 29;

i32 times_2(i32 val)
{
    return val + val;
}

i32 f(i32 val)
{
    return test + times_2(val);
}

i32 main()
{
    write( f(read()) );
    return 0;
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

    compiler.RegisterDeclarations(nodes);
    Console.WriteLine(compiler.CompileAll());
}
catch (CompilerException ex)
{
    Console.WriteLine(ex.Message);
}