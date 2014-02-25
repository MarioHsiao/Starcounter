
using SuperSocket.ClientEngine;
using System;
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
                    var state = WebSocketState.None;

                    EventHandler opened = (s, e) => {
                        socket.Send(handshake, 0, handshake.Length);
                        state = WebSocketState.Open;
                        x.Set();
                    };
                    EventHandler<ErrorEventArgs> errored = (s, e) => {
                        Console.WriteLine("Error: {0}, State: {1}", e.Exception.Message, socket.State);
                        x.Set();
                    };
                    
                    socket.Opened += opened;
                    socket.Error += errored;
                    socket.MessageReceived += (s, e) => {
                        if (messageCallback != null) {
                            messageCallback(this, e.Message);
                        }
                    };
                    socket.Closed += OnClosed;

                    socket.Open();
                    x.WaitOne();
                    socket.Opened -= opened;
                    socket.Error -= errored;

                    if (state == WebSocketState.Open) {
                        Console.WriteLine("Opened");
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

        void OnClosed(object sender, EventArgs args) {
            Console.WriteLine("Event on closed");
            closed.Set();
            if (!issuedClose) {
                closed.Reset();
                DoOpen();
            }
        }
    }
}
