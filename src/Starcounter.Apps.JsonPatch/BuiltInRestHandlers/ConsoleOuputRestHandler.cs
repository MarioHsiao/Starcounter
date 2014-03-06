﻿using Codeplex.Data;
using Starcounter.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.JsonPatch.BuiltInRestHandlers {
    public static class ConsoleOuputRestHandler {

        private static StreamWriter consoleWriter;

        /// <summary>
        /// List of WebSocket sessions
        /// </summary>
        static Dictionary<UInt64, WebSocket>[] WebSocketSessions = new Dictionary<UInt64, WebSocket>[Db.Environment.SchedulerCount];


        /// <summary>
        /// Unique identifier for WebSocket aka cargo ID.
        /// </summary>
        static UInt64[] UniqueWebSocketIdentifier = new UInt64[Db.Environment.SchedulerCount];


        /// <summary>
        /// List of Console Write Event's
        /// </summary>
        static ObservableCollection<ConsoleEventArgs> ConsoleWriteEvents = new ObservableCollection<ConsoleEventArgs>();


        /// <summary>
        /// Registers the built in REST handlers.
        /// </summary>
        /// <param name="defaultUserHttpPort">The public session data objects (view-models) are accessed using the same port as the user code REST handlers</param>
        /// <param name="defaultSystemHttpPort">The SQL access uses the system port</param>
        public static void Register(UInt16 defaultUserHttpPort, UInt16 defaultSystemHttpPort) {

            // Handle Console connections (Socket and non socket)
            Handle.GET(defaultSystemHttpPort, ScSessionClass.DataLocationUriPrefix + "console", (Request req) => {

                // Check if the request was a websocket request
                if (req.WebSocketUpgrade) {
                    Byte schedId = ThreadData.Current.Scheduler.Id;
                    UniqueWebSocketIdentifier[schedId]++;

                    WebSocket ws = req.SendUpgrade("console", UniqueWebSocketIdentifier[schedId]);
                    WebSocketSessions[schedId].Add(UniqueWebSocketIdentifier[schedId], ws);

                    ConsoleEvents consoleEvents = GetConsoleEvents();

                    // Pushing current console buffer.
                    ws.Send(consoleEvents.ToJson());

                    return HandlerStatus.Handled;
                }

                // Get and return the console buffer
                return ConsoleOuputRestHandler.GetConsoleResponse();
            });

            // Socket channel disconnected event
            Handle.SocketDisconnect(defaultSystemHttpPort, "console", (UInt64 cargoId, IAppsSession session) => {
                Byte schedId = ThreadData.Current.Scheduler.Id;
                if (WebSocketSessions[schedId].ContainsKey(cargoId))
                    WebSocketSessions[schedId].Remove(cargoId);
            });

            // Socket incoming message event
            Handle.Socket(defaultSystemHttpPort, "console", (String s, WebSocket ws) => {
                // We don't use incoming client messages.
            });

            // Setup console handling and callbacks to sessions etc..
            SetupConsoleHandling();
        }


        /// <summary>
        /// Setup The console handling
        /// Redirect the console output to a circular buffer with callback events
        /// </summary>
        /// <param name="WebSocketSessions"></param>
        private static void SetupConsoleHandling() {
            for (Byte i = 0; i < Db.Environment.SchedulerCount; i++) {
                WebSocketSessions[i] = new Dictionary<UInt64, WebSocket>();
            }

            CircularStream circularStream = new CircularStream(2048);
            circularStream.OnWrite += ConsoleOuputRestHandler.OnConsoleWrite;

            ConsoleOuputRestHandler.ConsoleWriteEvents.CollectionChanged += ConsoleOuputRestHandler.consoleEvents_CollectionChanged;

            // Redirect console output to circular memory buffer
            ConsoleOuputRestHandler.consoleWriter = new StreamWriter(circularStream);
            ConsoleOuputRestHandler.consoleWriter.AutoFlush = true;

            Console.SetOut(ConsoleOuputRestHandler.consoleWriter);
        }


        /// <summary>
        /// Event when a new console event has been made.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void consoleEvents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {


            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {

                // New console event
                // Send to listeners
                DbSession dbSession = new DbSession();

                for (Byte i = 0; i < Db.Environment.SchedulerCount; i++) {
                    Byte k = i;

                    dbSession.RunAsync(() => {

                        // Collect and create console events
                        foreach (ConsoleEventArgs consoleEventArg in e.NewItems) {

                            ConsoleEvent consoleEvent = new ConsoleEvent();
                            consoleEvent.databaseName = consoleEventArg.DatabaseName;
                            consoleEvent.applicationName = consoleEventArg.ApplicationName;
                            consoleEvent.text = consoleEventArg.Text;

                            ConsoleEvents consoleEvents = new ConsoleEvents();
                            consoleEvents.Items.Add(consoleEvent);

                            string s = consoleEvents.ToJson();

                            Byte sched = k;

                            // Send content to websocket listeners
                            foreach (KeyValuePair<UInt64, WebSocket> ws in WebSocketSessions[sched]) {
                                ws.Value.Send(s);
                            }
                        }
                    }, i);
                }

                // Limith the console event list size
                ObservableCollection<ConsoleEventArgs> list = sender as ObservableCollection<ConsoleEventArgs>;
                while (list.Count > 2000) {
                    list.RemoveAt(0);
                }
            }
        }


        /// <summary>
        /// Event when a console.write has been made
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnConsoleWrite(object sender, ConsoleEventArgs e) {
            // Add event to buffer
            ConsoleOuputRestHandler.ConsoleWriteEvents.Add(e);
        }


        /// <summary>
        /// Retrive the current console buffer 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Response GetConsoleResponse() {

            try {
                return GetConsoleEvents();
            }
            catch (Exception e) {

                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.message = e.Message;
                errorResponse.stackTrace = e.StackTrace;
                errorResponse.helpLink = e.HelpLink;

                return new Response() { BodyBytes = errorResponse.ToJsonUtf8(), StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError };
            }
        }


        /// <summary>
        /// Get console events
        /// </summary>
        /// <returns>ConsoleEvents</returns>
        private static ConsoleEvents GetConsoleEvents() {
            ConsoleEvents list = new ConsoleEvents();

            foreach (ConsoleEventArgs item in ConsoleWriteEvents) {
                ConsoleEvent consoleEvent = new ConsoleEvent();
                consoleEvent.databaseName = item.DatabaseName;
                consoleEvent.applicationName = item.ApplicationName;
                consoleEvent.text = item.Text;
                list.Items.Add(consoleEvent);
            }

            return list;
        }

    }


    /// <summary>
    /// Console Event
    /// </summary>
    internal class ConsoleEventArgs : EventArgs {
        public string DatabaseName { get; private set; }
        public string ApplicationName { get; private set; }
        public string Text { get; private set; }

        public ConsoleEventArgs(string databaseName, string applicationName, string text)
            : base() {
            this.DatabaseName = databaseName;
            this.ApplicationName = applicationName;
            this.Text = text;
        }

    }


    /// <summary>
    /// Circular buffer stream
    /// </summary>
    internal class CircularStream : Stream {

        #region Event

        /// <summary>
        /// Console write event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void WriteEventHandler(object sender, ConsoleEventArgs e);


        /// <summary>
        /// Console write event
        /// </summary>
        public event WriteEventHandler OnWrite;


        #endregion

        private long _Position = 0;
        private bool _IsBufferFull = false;
        private byte[] _Buffer;

        #region properties

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        #endregion


        public CircularStream(long size) {
            if (size <= 0) throw new ArgumentException("size");
            this._Buffer = new byte[size];
        }


        public override void Flush() {

        }

        public override long Length {
            get {
                if (this._IsBufferFull) return this._Buffer.Length;
                return this._Position;
            }
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Read from buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {

            lock (this) {
                long pos = 0;
                if (this._IsBufferFull) {
                    pos = this._Position;
                }

                int i = 0;
                for (; i < count && i <= (this.Length - 1); i++) {
                    buffer[offset + i] = this._Buffer[pos++];
                    if (pos > (this._Buffer.Length - 1)) {
                        pos = 0;
                    }
                }
                return i;
            }
        }


        /// <summary>
        /// Write to buffer and invoke event to listeners
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) {

            lock (this) {

                for (int i = 0; i < count; i++) {

                    _Buffer[this._Position++] = buffer[i];

                    if (this._Position == this._Buffer.Length) {
                        this._Position = 0;
                        this._IsBufferFull = true;
                    }
                }

                // Invoke listeners
                if (OnWrite != null) {
                    ConsoleEventArgs args = new ConsoleEventArgs(
                        StarcounterEnvironment.DatabaseNameLower,
                        this.GetAppName(),
                        System.Text.Encoding.Default.GetString(buffer, offset, count));

                    OnWrite(this, args);
                }

            }

        }


        /// <summary>
        /// Retrive Application name
        /// </summary>
        /// <returns>Starcounter Application Name</returns>
        private string GetAppName() {

            try {
                return Application.Current.Name;
            }
            catch (Exception) {
                return null;
            }
        }
    }

}
