using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace WebSocketsTestServer {

    //[Database]
    public class WebSocketState {
        public UInt64 Id;
        public Int32 NumMessagesToSend;
        public Int32 MessageSize;
        public Int32 NumMessagesSent;
        public Int32 NumMessagesReceived;
        public Int32 MessageLetter;
        public Boolean HasDisconnected;
    }

    class WebSocketsTestServer {

        static ConcurrentDictionary<UInt64, WebSocketState> allWebSockets_ = new ConcurrentDictionary<UInt64, WebSocketState>();

        const String WsTestGroupName = "WsTestGroup";

        const Int32 PushSleepInterval = 100;

        static UInt32 GlobalErrorCode = 0;

        static String GlobalErrorMessage = "";

        /// <summary>
        /// Processing incoming message.
        /// </summary>
        static void ProcessMessage(Byte[] data, WebSocket ws) {

            WebSocketState wss = allWebSockets_[ws.ToUInt64()];

            // Checking if there is no such WebSocket.
            if (wss == null) {

                GlobalErrorCode = 1;
                ws.Disconnect("Can't find WebSocket object with ID: " + ws.ToUInt64());
                return;
            }

            // Checking that message length is correct.
            if (data.Length != wss.MessageSize) {

                GlobalErrorCode = 2;
                ws.Disconnect("Wrong size of received data.");
                return;
            }

            // Checking that the received message is correct.
            for (Int32 i = 0; i < wss.MessageSize; i++) {

                if (data[i] != (Byte)wss.MessageLetter) {

                    GlobalErrorCode = 3;
                    ws.Disconnect("Wrong data in received WebSocket message.");
                    return;
                }
            }

            lock (WsTestGroupName) {

                // Incrementing the number of received messages.
                wss.NumMessagesReceived++;

            }
                
            // Pushing messages on this WebSocket.
            PushOnWebSocket(new String((Char)wss.MessageLetter, wss.MessageSize), ws);
        }

        /*
        /// <summary>
        /// Processing incoming message.
        /// </summary>
        static void ProcessMessage(Byte[] data, WebSocket ws) {

            WebSocketState wss = Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w WHERE w.Id=?", ws.ToUInt64()).First;

            // Checking if there is no such WebSocket.
            if (wss == null) {

                GlobalErrorCode = 1;
                ws.Disconnect("Can't find WebSocket state object in database.");
                return;
            }

            // Checking that message length is correct.
            if (data.Length != wss.MessageSize) {
                
                GlobalErrorCode = 2;
                ws.Disconnect("Wrong size of received data.");
                return;
            }

            // Checking that the received message is correct.
            for (Int32 i = 0; i < wss.MessageSize; i++) {

                if (data[i] != (Byte) wss.MessageLetter) {

                    GlobalErrorCode = 3;
                    ws.Disconnect("Wrong data in received WebSocket message.");
                    return;
                }
            }

            // Incrementing the number of received messages.
            Db.Transact(() => {
                wss.NumMessagesReceived++;
            });

            PushOnWebSocket(new String((Char)wss.MessageLetter, wss.MessageSize), ws);
        }

        /// <summary>
        /// Broadcasts on active sessions.
        /// </summary>
        static void BroadcastSessions(Int32 numForEachesPerRound) {

            while (true) {

                for (Int32 k = 0; k < numForEachesPerRound; k++) {

                    try {

                        Session.ForEach((Session s) => {

                            try {

                                // Getting active attached WebSocket.
                                WebSocket ws = s.ActiveWebSocket;
                                if (ws == null) {
                                    s.Destroy();
                                }

                                WebSocketState wss = Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w WHERE w.Id=?", ws.ToUInt64()).First;

                                // Checking if there is no such WebSocket.
                                if (wss == null) {
                                    GlobalErrorCode = 4;
                                    return;
                                }

                                // Pushing message on this WebSocket.
                                for (Int32 i = 0; i < 5; i++) {
                                    PushOnWebSocket(new String((Char)wss.MessageLetter, wss.MessageSize), ws);
                                }

                            } catch (Exception exc) {

                                GlobalErrorCode = 5;
                            }
                        });

                    } catch (Exception exc) {

                        GlobalErrorCode = 6;
                    }
                }

                Thread.Sleep(PushSleepInterval);
            }
        }

        /// <summary>
        /// Push a message to WebSocket.
        /// </summary>
        static void PushOnWebSocket(String message, WebSocket ws) {

            Db.Transact(() => {

                WebSocketState wss = Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w WHERE w.Id=?", ws.ToUInt64()).First;

                // Checking if have sent everything.
                if ((wss.NumMessagesReceived > 0) && (wss.NumMessagesSent < wss.NumMessagesToSend)) {

                    // Sending the message.
                    ws.Send(message);

                    // Incrementing the number of received messages.
                    wss.NumMessagesSent++;
                }
            });
        }

        Handle.WebSocketDisconnect(WsTestGroupName, (WebSocket ws) => {

            Db.Transact(() => {

                WebSocketState wss = Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w WHERE w.Id=?", ws.ToUInt64()).First;

                // Checking if there is no such WebSocket.
                if (wss == null) {
                    GlobalErrorCode = 12;
                    return;
                }

                wss.Delete();
            });
        });
         
        */

        /// <summary>
        /// Push a message to WebSocket.
        /// </summary>
        static void PushOnWebSocket(String message, WebSocket ws) {

            WebSocketState wss = allWebSockets_[ws.ToUInt64()];

            lock (WsTestGroupName) {

                // Checking if have sent everything.
                if ((wss.NumMessagesReceived > 0) && (wss.NumMessagesSent < wss.NumMessagesToSend)) {

                    // Sending the message.
                    ws.Send(message);

                    // Incrementing the number of received messages.
                    wss.NumMessagesSent++;
                }
            }
        }

        /// <summary>
        /// Broadcasts on active WebSockets.
        /// </summary>
        static void BroadcastWebSockets() {

            while (true) {

                for (Byte k = 0; k < StarcounterEnvironment.SchedulerCount; k++) {

                    try {

                        Byte s = k;

                        // Getting sessions for current scheduler.
                        new DbSession().RunAsync(() => {

                            /*foreach (WebSocketState wss in Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w")) {

                                WebSocket ws = new WebSocket(wss.Id);

                                // Pushing messages on this WebSocket.
                                for (Int32 i = 0; i < 5; i++) {
                                    PushOnWebSocket(new String((Char)wss.MessageLetter, wss.MessageSize), ws);
                                }
                            }*/

                            foreach (KeyValuePair<UInt64, WebSocketState> wssKey in allWebSockets_) {

                                WebSocketState wss = wssKey.Value;

                                WebSocket ws = new WebSocket(wss.Id);

                                // Pushing messages on this WebSocket.
                                for (Int32 i = 0; i < 5; i++) {
                                    PushOnWebSocket(new String((Char)wss.MessageLetter, wss.MessageSize), ws);
                                }
                            }

                        }, s);

                    } catch (Exception exc) {

                        GlobalErrorMessage = exc.ToString();
                        GlobalErrorCode = 4;
                    }
                }

                Thread.Sleep(PushSleepInterval);
            }
        }

        static Int32 Main(string[] args) {

            //Debugger.Launch();

            // Removing existing objects from database.
            /*Db.Transact(() => {
                Db.SlowSQL("DELETE FROM WebSocketState");
            });*/

            // Handle WebSocket connections for listening on log changes
            Handle.GET("/wstest", (Request req) => {

                try {

                    // Checking if its WebSocket upgrade.
                    if (req.WebSocketUpgrade) {

                        Int32 numMessagesToSend = Int32.Parse(req["NumMessagesToSend"]);
                        Int32 messageSize = Int32.Parse(req["MessageSize"]);
                        Int32 messageLetter = (Int32)req["MessageLetter"][0];

                        WebSocketState wss = null;

                        UInt64 wsId = req.GetWebSocketId();

                        //Db.Transact(() => {

                            wss = new WebSocketState() {
                                Id = wsId,
                                NumMessagesToSend = numMessagesToSend,
                                MessageSize = messageSize,
                                NumMessagesSent = 0,
                                NumMessagesReceived = 0,
                                MessageLetter = messageLetter,
                                HasDisconnected = false
                            };
                        //});

                        Dictionary<String, String> headers = new Dictionary<String, String>() {
                            { "WebSocketId", wsId.ToString() }
                        };

                        // Adding to dictionary.
                        allWebSockets_[wsId] = wss;

                        // Creating and attaching a new session.
                        Session s = new Session();

                        // Sending Web Socket upgrade.
                        WebSocket ws = req.SendUpgrade(WsTestGroupName, null, headers, s);

                        // First pushing WebSocket id.
                        ws.Send(wsId.ToString());

                        return HandlerStatus.Handled;
                    }

                    GlobalErrorCode = 5;

                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket upgrade on " + req.Uri + " was not approved."
                    };

                } catch (Exception exc) {

                    GlobalErrorMessage = exc.ToString();
                    GlobalErrorCode = 6;

                    return 500;
                }
            });

            Handle.WebSocketDisconnect(WsTestGroupName, (WebSocket ws) => {

                // Safely incrementing the global lock.
                WebSocketState wss = allWebSockets_[ws.ToUInt64()];

                if (wss == null) {
                    GlobalErrorCode = 7;
                    return;
                }

                lock (WsTestGroupName) {

                    wss.HasDisconnected = true;
                }
                
            });

            Handle.WebSocket(WsTestGroupName, (String message, WebSocket ws) => {

                Byte[] data = Encoding.UTF8.GetBytes(message);

                ProcessMessage(data, ws);
            });

            Handle.WebSocket(WsTestGroupName, (Byte[] data, WebSocket ws) => {

                ProcessMessage(data, ws);
            });

            Handle.GET("/WsStats/{?}", (UInt64 wsId) => {

                //WebSocketState wss = Db.SQL<WebSocketState>("SELECT w FROM WebSocketState w WHERE w.Id=?", wsId).First;
                WebSocketState wss = allWebSockets_[wsId];

                // Checking if there is no such WebSocket.
                if (wss == null) {
                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket with the following ID does not exist: " + wsId
                    };
                }

                String stats = String.Format("{0} {1} {2} {3} {4} {5} {6}", 
                    wss.Id,
                    wss.NumMessagesToSend,
                    wss.MessageSize,
                    wss.NumMessagesSent,
                    wss.NumMessagesReceived,
                    (Char) wss.MessageLetter,
                    wss.HasDisconnected);

                Boolean removed = allWebSockets_.TryRemove(wsId, out wss);

                if (!removed) {

                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket with the following ID can't be removed: " + wsId
                    };
                }

                return stats;
            });

            Handle.GET("/WsGlobalStatus", () => {

                // Checking if there was an error during the test run.
                if (GlobalErrorCode != 0) {

                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket test failed",
                        Body = GlobalErrorCode.ToString() + ": " + GlobalErrorMessage.ToString()
                    };
                }

                return 200;
            });

            //Thread broadcastSessionsThread = new Thread(() => { BroadcastSessions(); });
            //broadcastSessionsThread.Start();

            Thread broadcastWebSocketsThread = new Thread(() => { BroadcastWebSockets(); });
            broadcastWebSocketsThread.Start();

            return 0;
        }
    }
}
