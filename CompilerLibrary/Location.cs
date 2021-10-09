namespace CompilerLibrary
{
    /// <summary>
    /// Represents a code element's location within the source
    /// </summary>
    public record Location(string FilePath, int Line, int Column, int Length)
    {
        public override string ToString()
            => $"File \"{FilePath}\" at {Line}:{Column} length {Length}";
    }
}
