using System;

namespace CompilerLibrary;

/// <summary>
/// Represents a compiler error that specifies
/// the location of the error within the source
/// </summary>
public abstract class CompilerException : Exception
{
    /// <summary>
    /// The location of the error within the source
    /// </summary>
    public Location Location { get; init; }

    /// <summary>
    /// The error message without the location
    /// </summary>
    public string TrueMessage { get; init; }

    public CompilerException(Location location, string message)
        : base($"{location}: {message}")
    {
        Location = location;
        TrueMessage = message;
    }
}