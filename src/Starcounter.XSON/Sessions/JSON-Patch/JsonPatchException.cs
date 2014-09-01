using System;

namespace Starcounter.XSON {
    public class JsonPatchException : Exception {
        private string patch;

        internal JsonPatchException(string message, string patch)
            : base(message) {
            this.patch = patch;
        }

        public string Patch {
            get { return patch; }
            internal set { patch = value; }
        }
    }
}
