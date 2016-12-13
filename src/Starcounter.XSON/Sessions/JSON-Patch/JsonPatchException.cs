using System;
using System.Text;

namespace Starcounter.XSON {
    public class JsonPatchException : Exception {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public JsonPatchException(string message)
            : base(message, null) {
            Severity = 0;
        }

        public JsonPatchException(string message, Exception innerException)
            : base(message, innerException) {
            Severity = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public JsonPatchException(int severity, string message)
            : base(message) {
            Severity = severity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="patch"></param>
        internal JsonPatchException(int severity, string message, string patch)
            : base(message) {
            Patch = patch;
            Severity = severity;
        }

        /// <summary>
        /// The patch that was currently applied when the exception was thrown.
        /// </summary>
        public string Patch {
            get;
            set;
        }

        /// <summary>
        /// Contains the current client and server versions if versioning is enabled.
        /// </summary>
        public ViewModelVersion Version {
            get;
            set;
        }

        /// <summary>
        /// Contains the value of the current property when moving through the patch.
        /// </summary>
        public string CurrentProperty {
            get;
            set;
        }

        // TODO:
        // Refactor
        public int Severity {
            get;
            private set;
        }

        public override string Message {
            get {
                return FormatDetailedMessage(base.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string FormatDetailedMessage(string message) {
            StringBuilder sb = new StringBuilder();

            if (message != null) {
                sb.Append(message);

                if (!message.EndsWith("."))
                    sb.Append('.');
            }

            if (Patch != null) {
                sb.Append(" Patch: '");
                sb.Append(Patch);
                sb.Append("'.");
            }

            if (CurrentProperty != null) {
                sb.Append(" Property: '");
                sb.Append(CurrentProperty);
                sb.Append("'.");
            }

            var version = Version;
            if (version != null) {
                sb.Append(" Viewmodel versions: {");
                sb.Append("client (");
                sb.Append(version.RemoteVersionPropertyName);
                sb.Append("): ");
                sb.Append(version.RemoteVersion);
                sb.Append(", clients serverversion: ");
                sb.Append(version.RemoteLocalVersion);
                sb.Append(", server (");
                sb.Append(version.LocalVersionPropertyName);
                sb.Append("): ");
                sb.Append(version.LocalVersion);
                sb.Append("}.");
            }

            return sb.ToString();
        }
    }
}
