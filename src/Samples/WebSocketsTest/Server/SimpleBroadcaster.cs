using Starcounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebSocketsTestServer {

    [Database]
    public class WebSocketId {
        public UInt64 Id;
        public UInt32 NumBroadcasted;
    }

    class SimpleBroadcaster {

        // NOTE: Timer should be static, otherwise its garbage collected.
        static Timer WebSocketSessionsTimer = null;

        static Int32 Main(string[] args) {

            // Removing existing objects from database.
            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM WebSocketId");
            });

            WebSocketSessionsTimer = new Timer((state) => {

                // Getting sessions for current scheduler.
                new DbSession().RunAsync(() => {

                    Db.Transact(() => {

                        foreach (WebSocketId wsDb in Db.SQL("SELECT w FROM WebSocketId w")) {

                            WebSocket ws = new WebSocket(wsDb.Id);

                            // Checking if ready to disconnect after certain amount of sends.
                            if (wsDb.NumBroadcasted == 100) {

                                ws.Disconnect();
                                wsDb.Delete();

                                // Proceeding to next active WebSocket.
                                continue;
                            }

                            String sendMsg = "Broadcasting id: " + wsDb.Id;

                            ws.Send(sendMsg);

                            wsDb.NumBroadcasted++;
                        }
                    });

                });

            }, null, 100, 100);

            // Registering WebSocket handler.
            Handle.GET("/ws", (Request req) => {

                if (req.WebSocketUpgrade) {

                    WebSocket ws = req.SendUpgrade("WsGroup1");

                    // Save wsIdlower, wsIdUpper in database.
                    Db.Transact(() => {
                        new WebSocketId() {
                            Id = ws.ToUInt64()
                        };
                    });

                    return HandlerStatus.Handled;
                }

                return new Response() {
                    StatusCode = 500,
                    StatusDescription = "Not a WebSocket upgrade."
                };
            });

            Handle.WebSocket("WsGroup1", (String s, WebSocket ws) => {

                UInt64 id = ws.ToUInt64();

                WebSocket testWsToSend = new WebSocket(id);

                testWsToSend.Send(s);
            });

            Handle.WebSocket("WsGroup1", (Byte[] s, WebSocket ws) => {
                ws.Send(s);
            });

            Handle.WebSocketDisconnect("WsGroup1", (WebSocket ws) => {

                Db.Transact(() => {

                    WebSocketId wsId = Db.SQL<WebSocketId>("SELECT w FROM WebSocketId w WHERE w.Id=?", ws.ToUInt64()).First;
                    if (wsId != null) {
                        wsId.Delete();
                    }
                });
            });

            return 0;
        }
    }
}
