using System;

namespace Starcounter.XSON {
    public class JsonPatchException : Exception {
        private string patch;
        private int severity;

        public JsonPatchException(string message)
            : base(message) {
            this.patch = null;
            this.severity = 0;
        }

        public JsonPatchException(int severity, string message) : base(message) {
            this.patch = null;
            this.severity = severity;
        }

        internal JsonPatchException(int severity, string message, string patch)
            : base(message) {
            this.patch = patch;
            this.severity = severity;
        }

        public string Patch {
            get { return patch; }
            internal set { patch = value; }
        }

        public int Severity {
            get { return severity; }
        }
    }
}
