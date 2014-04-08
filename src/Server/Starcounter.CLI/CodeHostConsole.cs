
using Starcounter.JsonPatch.BuiltInRestHandlers;
using SuperSocket.ClientEngine;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Starcounter.CLI {
    using System.Globalization;
    using Task = System.Threading.Tasks.Task;
    using WebSocket = WebSocket4Net.WebSocket;

    /// <summary>
    /// Implements a console bound to a given code host.
    /// </summary>
    public sealed class CodeHostConsole {
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

        /// <summary>
        /// Filters output based on a given time.
        /// </summary>
        public readonly DateTime TimeFilter;

        /// <summary>
        /// Filters only output from a specified application.
        /// </summary>
        public readonly string ApplicationName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="timeFilter"></param>
        /// <param name="applicationName"></param>
        public CodeHostConsole(string databaseName, DateTime? timeFilter = null, string applicationName = null) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }
            DatabaseName = databaseName;
            TimeFilter = timeFilter.HasValue ? timeFilter.Value : DateTime.MinValue;
            ApplicationName = applicationName;
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<CodeHostConsole> Opened {
            set {
                openedCallback = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<CodeHostConsole> Closed {
            set {
                closedCallback = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<CodeHostConsole, string> MessageWritten {
            set {
                messageCallback = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Open() {
            DoOpen();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close() {
            closeIssued = true;
            opening.Wait();

            if (socket.State == WebSocketState.Open) {
                socket.Close();
            }
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
                if (QualifiesThroughFilter(item)) {
                    callback(this, item.text);
                }
            }
        }

        bool QualifiesThroughFilter(ConsoleEvents.ItemsElementJson consoleEvent) {
            var appFilter = ApplicationName;

            // Apply application filter if given
            if (!string.IsNullOrEmpty(appFilter)) {
                if (!string.IsNullOrEmpty(consoleEvent.applicationName)) {
                    if (!appFilter.Equals(consoleEvent.applicationName, StringComparison.InvariantCultureIgnoreCase)) {
                        return false;
                    }
                }
            }

            // Keep the time-filter somewhat relaxed. If we get some input we can't
            // properly interpret, we return a positive rather than not.
            DateTime time;
            var parsed = DateTime.TryParse(consoleEvent.time, null, DateTimeStyles.RoundtripKind, out time);
            if (parsed) return time > TimeFilter;
            return true;
        }
    }
}