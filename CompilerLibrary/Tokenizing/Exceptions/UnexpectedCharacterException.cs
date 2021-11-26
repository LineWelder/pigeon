namespace CompilerLibrary.Tokenizing.Exceptions;

public class UnexpectedCharacterException : CompilerException
{
    public char Character { get; init; }

    public UnexpectedCharacterException(Location location, char character)
        : base(location, $"Unexpected character '{character}'") 
    {
        Character = character;
    }
}