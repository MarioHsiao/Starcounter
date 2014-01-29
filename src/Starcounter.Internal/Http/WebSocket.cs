using Starcounter;
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter
{
    class WebSocket
    {
        // Reference to corresponding session.
        public IAppsSession Session
        {
            get;
            set;
        }

        // Disconnects existing socket.
        public void Disconnect()
        {

        }

        // Executes given user delegate on all active sockets.
        public static void RunOnAllActiveWebSockets(Action<WebSocket> userDelegate, String channel)
        {

        }

        // Server push on WebSocket.
        public void Send(Byte[] data, Boolean isText = false, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags)
        {
            Request req = Request.GenerateNewRequest(Session.InternalSession, MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS, isText);

            req.SendResponse(data, 0, data.Length, connFlags);

        }

        // Server push on WebSocket.
        public void Send(String data, Boolean isText = true, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags)
        {
            Send(Encoding.UTF8.GetBytes(data), isText, connFlags);
        }
    }
}