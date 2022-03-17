using System;
using System.IO;
using System.Text;
using CompilerLibrary;
using CompilerLibrary.Compiling;
using CompilerLibrary.Parsing;
using CompilerLibrary.Tokenizing;

const string code = @"
i32 test = 0;

i32 test_plus_2()
{
    return test + 2;
}

i32 main()
{
    test = input;
    return test_plus_2();
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

    // FunctionDeclarationNode main = nodes[4] as FunctionDeclarationNode;
    // AssignmentNode assignment = main?.Body?[0] as AssignmentNode;
    // SyntaxNode expression = assignment?.Right;
    // SyntaxNode optimized = Optimizer.OptimizeExpression(expression);
    
    // Debug.PrintSyntaxNode(expression);
    // Console.WriteLine();
    // Debug.PrintSyntaxNode(optimized);
    // Console.WriteLine();

    compiler.RegisterDeclarations(nodes);
    Console.WriteLine(compiler.CompileAll());
}
catch (CompilerException ex)
{
    Console.WriteLine(ex.Message);
}