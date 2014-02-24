
using System;
using WebSocket4Net;

namespace staradmin {

    /// <summary>
    /// Implements a console bound to a given code host.
    /// </summary>
    internal sealed class CodeHostConsole {
        Action<CodeHostConsole> openedCallback;
        Action<CodeHostConsole> closedCallback;
        Action<CodeHostConsole, string> messageCallback;
        WebSocket socket;

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
            socket = new WebSocket(string.Format("ws://localhost:8181/__{0}/console/ws", DatabaseName.ToLowerInvariant()));
            
            var handshake = new byte[1] { 0 };
            socket.Opened += (s, e) => {
                socket.Send(handshake, 0, handshake.Length);
                if (openedCallback != null) {
                    openedCallback(this);
                }
            };

            socket.Open();
        }

        public void Close() {
            throw new NotImplementedException();
        }
    }
}
