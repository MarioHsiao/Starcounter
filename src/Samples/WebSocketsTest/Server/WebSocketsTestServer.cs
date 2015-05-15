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
    }

    class WebSocketsTestServer {

        static ConcurrentDictionary<UInt64, WebSocketState> allWebSockets_ = new ConcurrentDictionary<UInt64, WebSocketState>();

        const String WsTestGroupName = "WsTestGroup";

        const Int32 PushSleepInterval = 100;

        static Int32 GlobalErrorCode = 0;

        static Int32 GlobalNumberOfDisconnects = 0;

        /// <summary>
        /// Processing incoming message.
        /// </summary>
        static void ProcessMessage(Byte[] data, WebSocket ws) {

            WebSocketState wss = allWebSockets_[ws.ToUInt64()];

            // Checking if there is no such WebSocket.
            if (wss == null) {

                ws.Disconnect("Can't find WebSocket state object in database.");
                GlobalErrorCode = 1;
                return;
            }

            // Checking that message length is correct.
            if (data.Length != wss.MessageSize) {

                ws.Disconnect("Wrong size of received data.");
                GlobalErrorCode = 2;
                return;
            }

            // Checking that the received message is correct.
            for (Int32 i = 0; i < wss.MessageSize; i++) {

                if (data[i] != (Byte)wss.MessageLetter) {

                    ws.Disconnect("Wrong data in received WebSocket message.");
                    GlobalErrorCode = 3;
                    return;
                }
            }

            // Incrementing the number of received messages.
            wss.NumMessagesReceived++;
                
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

                ws.Disconnect("Can't find WebSocket state object in database.");
                GlobalErrorCode = 1;
                return;
            }

            // Checking that message length is correct.
            if (data.Length != wss.MessageSize) {

                ws.Disconnect("Wrong size of received data.");
                GlobalErrorCode = 2;
                return;
            }

            // Checking that the received message is correct.
            for (Int32 i = 0; i < wss.MessageSize; i++) {

                if (data[i] != (Byte) wss.MessageLetter) {

                    ws.Disconnect("Wrong data in received WebSocket message.");
                    GlobalErrorCode = 3;
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
                                Console.WriteLine("Error occurred: " + exc.ToString());
                            }
                        });

                    } catch (Exception exc) {

                        GlobalErrorCode = 6;
                        Console.WriteLine("Error occurred: " + exc.ToString());
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
        static void BroadcastWebSockets(Int32 numForEachesPerRound) {

            while (true) {

                Byte schedId = 0;

                for (Int32 k = 0; k < numForEachesPerRound; k++) {

                    try {

                        Byte s = schedId;

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

                        schedId++;
                        if (schedId >= StarcounterEnvironment.SchedulerCount) {
                            schedId = 0;
                        }

                    } catch (Exception exc) {

                        GlobalErrorCode = 9;
                        Console.WriteLine("Error occurred: " + exc.ToString());
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

                        GlobalErrorCode = 0;

                        WebSocketState wss = null;

                        UInt64 wsId = req.GetWebSocketId();

                        //Db.Transact(() => {

                            wss = new WebSocketState() {
                                Id = wsId,
                                NumMessagesToSend = numMessagesToSend,
                                MessageSize = messageSize,
                                NumMessagesSent = 0,
                                NumMessagesReceived = 0,
                                MessageLetter = messageLetter
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

                    GlobalErrorCode = 10;

                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket upgrade on " + req.Uri + " was not approved."
                    };

                } catch (Exception exc) {

                    GlobalErrorCode = 11;
                    Console.WriteLine("Error occurred: " + exc.ToString());

                    return 500;
                }
            });

            Handle.WebSocketDisconnect(WsTestGroupName, (WebSocket ws) => {
                /*
                // Safely incrementing the global lock.
                WebSocketState wss;
                Boolean removed = allWebSockets_.TryRemove(ws.ToUInt64(), out wss);

                if (!removed) {
                    GlobalErrorCode = 12;
                    return;
                }
                */
                lock (WsTestGroupName) {
                    GlobalNumberOfDisconnects++;
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
                    GlobalErrorCode = 12;
                    return 400;
                }

                String s = String.Format("{0} {1} {2} {3} {4} {5}", 
                    wss.Id,
                    wss.NumMessagesToSend,
                    wss.MessageSize,
                    wss.NumMessagesSent,
                    wss.NumMessagesReceived,
                    (Char) wss.MessageLetter);

                return s;
            });

            Handle.GET("/WsGlobalStatus", () => {

                // Checking if there was an error during the test run.
                if (GlobalErrorCode != 0) {

                    return new Response() {
                        StatusCode = 500,
                        StatusDescription = "WebSocket test failed",
                        Body = GlobalErrorCode.ToString()
                    };
                }

                Int32 totalNumDisconnects = GlobalNumberOfDisconnects;
                GlobalNumberOfDisconnects = 0;

                return new Response() {
                    Body = String.Format("{0}", totalNumDisconnects)
                };
            });

            //Thread broadcastSessionsThread = new Thread(() => { BroadcastSessions(10); });
            //broadcastSessionsThread.Start();

            //Thread broadcastWebSocketsThread = new Thread(() => { BroadcastWebSockets(10); });
            //broadcastWebSocketsThread.Start();

            return 0;
        }
    }
}
