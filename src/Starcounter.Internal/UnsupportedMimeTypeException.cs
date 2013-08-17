
using System;
namespace Starcounter.Advanced {
    /// <summary>
    /// 
    /// </summary>
    public class UnsupportedMimeTypeException : Exception {
        public UnsupportedMimeTypeException(string m)
            : base(m) {
        }
    }
}
