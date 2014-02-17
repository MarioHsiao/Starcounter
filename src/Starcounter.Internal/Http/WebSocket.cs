using Starcounter;
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter
{
    public class WebSocket
    {
        // Reference to corresponding session.
        public IAppsSession Session
        {
            get;
            set;
        }

        /// <summary>
        /// Specific saved user object ID.
        /// </summary>
        public UInt64 CargoId
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

        String message_;

        internal String Message
        {
            get
            {
                return message_;
            }

            set
            {
                message_ = value;
            }
        }

        Byte[] bytes_;

        internal Byte[] Bytes
        {
            get
            {
                return bytes_;
            }

            set
            {
                bytes_ = value;
            }
        }

        NetworkDataStream dataStream_;

        internal WebSocket(NetworkDataStream dataStream, String message, Byte[] bytes)
        {
            dataStream_ = dataStream;
            message_ = message;
            bytes_ = bytes;
        }
    }
}