
using System;
using System.Runtime.Serialization;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Exception thrown when a set of command line arguments doesn't match
    /// the valid syntax or semantics of a program definition.
    /// </summary>
    [Serializable]
    public class InvalidCommandLineException : Exception
    {
        public uint ErrorCode { get; internal set; }

        internal InvalidCommandLineException() { }

        internal InvalidCommandLineException(string message) : base(message) { }
        internal InvalidCommandLineException(string message, Exception innerException) : base(message, innerException) { }

        protected InvalidCommandLineException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}