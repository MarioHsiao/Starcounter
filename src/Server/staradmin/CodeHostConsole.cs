
using System;

namespace staradmin {

    /// <summary>
    /// Implements a console bound to a given code host.
    /// </summary>
    internal sealed class CodeHostConsole {
        Action<CodeHostConsole> openedCallback;
        Action<CodeHostConsole> closedCallback;
        Action<CodeHostConsole, string> messageCallback;

        /// <summary>
        /// Gets the name of the database running in the code host
        /// whose console the current instance represent.
        /// </summary>
        public readonly string DatabaseName;

        public CodeHostConsole(string databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }
            DatabaseName = databaseName;
        }

        public Action<CodeHostConsole> Opened {
            set {
                openedCallback = value;
            }
        }

        public Action<CodeHostConsole> Closed {
            set {
                closedCallback = value;
            }
        }

        public Action<CodeHostConsole, string> MessageWritten {
            set {
                messageCallback = value;
            }
        }

        public void Open() {
            throw new NotImplementedException();
        }

        public void Close() {
            throw new NotImplementedException();
        }
    }
}
