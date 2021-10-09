namespace CompilerLibrary.Tokenizing.Exceptions
{
    public class UnexpectedCharacterException : CompilerException
    {
        public UnexpectedCharacterException(Location location, char character)
            : base(location, $"Unexpected character '{character}'") { }
    }
}
