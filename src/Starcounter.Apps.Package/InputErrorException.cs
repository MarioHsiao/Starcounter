using System;

namespace Starcounter.Apps.Package {
    /// <summary>
    /// InputErrorException
    /// </summary>
    [SerializableAttribute]
    public class InputErrorException : Exception {
        public InputErrorException() { }
        public InputErrorException(string message) : base(message) { }
    }
}
