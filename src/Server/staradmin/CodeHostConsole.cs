
using SuperSocket.ClientEngine;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace staradmin {

    /// <summary>
    /// Implements a console bound to a given code host.
    /// </summary>
    internal sealed class CodeHostConsole {
        Action<CodeHostConsole> openedCallback;
        Action<CodeHostConsole> closedCallback;
        Action<CodeHostConsole, string> messageCallback;
        ManualResetEvent closed = new ManualResetEvent(false);
        WebSocket socket;
        bool issuedClose = false;

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
            DoOpen();
        }

        void DoOpen() {
            Task.Run(() => {
                var x = new AutoResetEvent(false);
                var handshake = new byte[1] { 0 };

                while (true) {
                    socket = new WebSocket(string.Format("ws://localhost:8181/__{0}/console/ws", DatabaseName.ToLowerInvariant()));
                    
                    EventHandler opened = (s, e) => {
                        socket.Send(handshake, 0, handshake.Length);
                        x.Set();
                    };
                    EventHandler<ErrorEventArgs> errored = (s, e) => {
                        Trace.WriteLine("Error when opening web socket: {0}", e.Exception.Message);
                        x.Set();
                    };

                    socket.Opened += opened;
                    socket.Error += errored;
                    socket.DataReceived += (s, e) => {
                        InvokeMessageCallback(e.Data);
                    };
                    socket.MessageReceived += (s, e) => {
                        InvokeMessageCallback(e.Message);
                    };
                    socket.Closed += OnClosed;

                    socket.Open();
                    x.WaitOne();

                    socket.Opened -= opened;
                    socket.Error -= errored;

                    if (socket.State == WebSocketState.Open) {
                        socket.Error += OnError;
                        if (openedCallback != null) {
                            openedCallback(this);
                        }
                        break;
                    }
                }
            });
        }

        public void Close() {
            if (socket.State == WebSocketState.Open) {
                issuedClose = true;
                socket.Close();
                closed.WaitOne();
            }
        }

        void OnError(object sender, ErrorEventArgs error) {
            Trace.WriteLine("Error on web socket connection: {0}", error.Exception.Message);
            closed.Set();
            if (!issuedClose) {
                closed.Reset();
                DoOpen();
            }
        }

        void OnClosed(object sender, EventArgs args) {
            Trace.WriteLine("Web socket connection closed (Issued: {0})", issuedClose ? bool.TrueString : bool.FalseString);
            closed.Set();
            if (!issuedClose) {
                closed.Reset();
                DoOpen();
            }
        }

        void InvokeMessageCallback(byte[] data) {
            if (messageCallback != null) {
                messageCallback(this, Encoding.UTF8.GetString(data));
            }
        }

        void InvokeMessageCallback(string message) {
            if (messageCallback != null) {
                messageCallback(this, message);
            }
        }
    }
}
