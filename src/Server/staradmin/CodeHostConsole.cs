
using Starcounter.JsonPatch.BuiltInRestHandlers;
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
        Task opening;
        Action<CodeHostConsole> openedCallback;
        Action<CodeHostConsole> closedCallback;
        Action<CodeHostConsole, string> messageCallback;
        ManualResetEvent closed = new ManualResetEvent(false);
        WebSocket socket;
        volatile bool closeIssued = false;

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
            opening = Task.Run(() => {
                var x = new AutoResetEvent(false);
                
                while (!closeIssued) {
                    socket = new WebSocket(string.Format("ws://localhost:8181/__{0}/console", DatabaseName.ToLowerInvariant()));
                    
                    EventHandler opened = (s, e) => {
                        x.Set();
                    };
                    EventHandler<ErrorEventArgs> errored = (s, e) => {
                        Trace.WriteLine(string.Format("Error when opening web socket: {0}", e.Exception.Message));
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
            closeIssued = true;
            opening.Wait();

            if (socket.State == WebSocketState.Open) {
                socket.Close();
            }
        }

        void OnError(object sender, ErrorEventArgs error) {
            Trace.WriteLine(string.Format("Error on web socket connection: {0}", error.Exception.Message));
            if (!closeIssued) {
                DoOpen();
            }
        }

        void OnClosed(object sender, EventArgs args) {
            Trace.WriteLine(
                string.Format("Web socket connection closed (Issued: {0})", closeIssued ? bool.TrueString : bool.FalseString));
            if (!closeIssued) {
                DoOpen();
            } else if (closedCallback != null) {
                closedCallback(this);
            }
        }

        void InvokeMessageCallback(byte[] data) {
            if (messageCallback != null) {
                UnpackAndInvokeMessageCallback(messageCallback, Encoding.UTF8.GetString(data));
            }
        }

        void InvokeMessageCallback(string message) {
            if (messageCallback != null) {
                UnpackAndInvokeMessageCallback(messageCallback, message);
            }
        }

        void UnpackAndInvokeMessageCallback(Action<CodeHostConsole, string> callback, string content) {
            var events = new ConsoleEvents();
            events.PopulateFromJson(content);

            foreach (ConsoleEvents.ItemsElementJson item in events.Items) {
                callback(this, item.text);
            }
        }
    }
}
