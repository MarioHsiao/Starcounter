
using System;

namespace Sc.Tools.Logging
{

/// <summary>
/// Exception thrown if unable to read a Starcounter log file because the
/// file wasn't compatible with any known Starcounter log file format.
/// </summary>
public sealed class NotALogFileException : Exception
{

    internal NotALogFileException(String message) : base(message) {}
}
}
