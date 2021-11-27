namespace CompilerLibrary.Compiling.Exceptions;

public class OutOfRegistersException : CompilerException
{
    public OutOfRegistersException(Location location)
        : base(location, "Unable to allocate a register, no free registers left") { }
}