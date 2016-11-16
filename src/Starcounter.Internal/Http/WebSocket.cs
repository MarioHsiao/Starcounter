using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter
{
    public class WebSocket
    {
        /// <summary>
        /// Type of handler for WebSocket.
        /// </summary>
        internal enum WsHandlerType
        {
            StringMessage,
            BinaryData,
            Disconnect,
            Empty
        }

        /// <summary>
        /// WebSocket close codes.
        /// </summary>
        public enum WebSocketCloseCodes {
            WS_CLOSE_NORMAL = 1000,
            WS_CLOSE_GOING_DOWN = 1001,
            WS_CLOSE_PROTOCOL_ERROR = 1002,
            WS_CLOSE_CANT_ACCEPT_DATA = 1003,
            WS_CLOSE_WRONG_DATA_TYPE = 1007,
            WS_CLOSE_POLICY_VIOLATED = 1008,
            WS_CLOSE_MESSAGE_TOO_BIG = 1009,
            WS_CLOSE_UNEXPECTED_CONDITION = 1011
        }

        /// <summary>
        /// Current WebSocket object.
        /// </summary>
        [ThreadStatic]
        internal static WebSocket Current_;

        /// <summary>
        /// Current WebSocket object.
        /// </summary>
        public static WebSocket Current
        {
            get { return Current_; }
            set { Current_ = value; }
        }

        /// <summary>
        /// Socket structure.
        /// </summary>
        SocketStruct socketStruct_;

        /// <summary>
        /// Socket structure.
        /// </summary>
        internal SocketStruct InternalSocketStruct {
            get {
                return socketStruct_;
            }
        }

        /// <summary>
        /// Reference to existing session if any.
        /// </summary>
        public IAppsSession Session {
            get;
            internal set;
        }

        Boolean isText_;

        /// <summary>
        /// Is a text or binary message?
        /// </summary>
        internal Boolean IsText {
            get {
                return isText_;
            }
        }

        String message_;

        /// <summary>
        /// Received text message.
        /// </summary>
        internal String Message {
            get {
                return message_;
            }
        }

        Byte[] bytes_;

        /// <summary>
        /// Received binary bytes.
        /// </summary>
        internal Byte[] Bytes {
            get {
                return bytes_;
            }
        }

        WsHandlerType wsHandlerType_;

        internal WsHandlerType HandlerType {
            get { return wsHandlerType_; }
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        public Boolean IsDead() {

            return socketStruct_.IsDead();
        }

        /// <summary>
        /// Disconnects active WebSocket.
        /// </summary>
        /// <param name="message">Optional error message.</param>
        /// <param name="code">Optional error code.</param>
        public void Disconnect(String message = null, WebSocketCloseCodes code = WebSocketCloseCodes.WS_CLOSE_NORMAL) {

            // Checking if WebSocket is valid.
            if (IsDead())
                return;

            Int32 bufLen = 2;            

            if (message != null)
                bufLen += message.Length * 2;

            Byte[] buf = new Byte[bufLen];

            Int32 bytesWritten = 0;

            unsafe
            {
                fixed (byte* p = buf)
                {
                    // Writing error code in network order.
                    *(Int16*)p = IPAddress.HostToNetworkOrder((Int16)code);
                    bytesWritten += 2;

                    // Writing status description if any.
                    if (null != message)
                    {
                        Utf8Writer writer = new Utf8Writer(p + 2);

                        writer.Write(message);
                        bytesWritten += writer.Written;
                    }
                }
            }

            PushServerMessage(buf, bytesWritten, true, Response.ConnectionFlags.GracefullyCloseConnection);

            socketStruct_.Kill();
        }

        /// <summary>
        /// Streaming over TCP socket.
        /// NOTE: Function closes the stream once the end of stream is reached.
        /// </summary>
        internal async Task SendStreamOverSocket(Stream whatToStream, Byte[] fetchBuffer, bool isText = false) {

            try {

                Int32 numBytesRead = await whatToStream.ReadAsync(fetchBuffer, 0, fetchBuffer.Length);

                // Checking if its the end of the stream.
                if (0 == numBytesRead) {
                    whatToStream.Close();
                    return;
                }

                // We need to be on scheduler to send on socket.
                StarcounterBase._DB.RunAsync(() => {

                    // Sending on socket.
                    Send(fetchBuffer, numBytesRead, isText);

                    // Scheduling a new task to read from the stream.
                    Task.Run(() => SendStreamOverSocket(whatToStream, fetchBuffer));
                });

            } catch (Exception exc) {

                // Just logging the exception.
                Diagnostics.LogHostException(exc);
            }
        }

        /// <summary>
        /// Server push on WebSocket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
        public void Send(Byte[] data, Boolean isText = false, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags) {
            PushServerMessage(data, data.Length, isText, connFlags);
        }

        /// <summary>
        /// Server push on WebSocket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        /// <param name="dataLen">Length of data in bytes.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
        public void Send(Byte[] data, Int32 dataLen, Boolean isText = false, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags) {
            PushServerMessage(data, dataLen, isText, connFlags);
        }

        /// <summary>
        /// Server push on WebSocket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
        public void Send(String data, Boolean isText = true, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags) {
            Send(Encoding.UTF8.GetBytes(data), isText, connFlags);
        }

        /// <summary>
        /// Internal WebSocket creation.
        /// </summary>
        internal WebSocket(
            SocketStruct socketStruct,
            String message,
            Byte[] bytes,
            Boolean isText,
            WsHandlerType wsHandlerType)
        {
            socketStruct_ = socketStruct;
            message_ = message;
            bytes_ = bytes;
            wsHandlerType_ = wsHandlerType;
            isText_ = isText;
        }

        /// <summary>
        /// Converts socket struct to lower and upper parts.
        /// </summary>
        public UInt64 ToUInt64() {
            return socketStruct_.ToUInt64();
        }

        /// <summary>
        /// Converts UInt64 WebSocket ID into an object.
        /// </summary>
        public WebSocket(UInt64 socketId) {
            socketStruct_.FromUInt64(socketId);
        }

        /// <summary>
        /// Converts lower and upper part of WebSocket into an object.
        /// </summary>
        internal WebSocket(NetworkDataStream dataStream) {

            socketStruct_.Init(dataStream);
        }

        /// <summary>
        /// Destroys the socket.
        /// </summary>
        internal void Destroy(Boolean isStarcounterThread) {

            // Disconnecting dead WebSocket from the session.
            if (Session != null) {
                Session.ActiveWebSocket = null;
            }

            socketStruct_.Kill();
        }
       
        /// <summary>
        /// Creates new Request based on session.
        /// </summary>
        internal unsafe void PushServerMessage(
            Byte[] data,
            Int32 dataLen,
            Boolean isText,
            Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags)
        {
            // Checking if WebSocket is valid.
            if (IsDead())
                return;

            UInt32 chunkIndex;
            Byte* chunkMem;

            // Checking if we still have the data stream with original chunk available.
            UInt32 errCode = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);

            if (0 != errCode) {

                if (Error.SCERRACQUIRELINKEDCHUNKS == errCode) {
                    // NOTE: If we can not obtain a chunk just returning because we can't do much.
                    return;
                } else {
                    throw ErrorCode.ToException(errCode);
                }
            }

            // Creating network data stream object.
            NetworkDataStream dataStream = new NetworkDataStream(
                chunkIndex, 
                socketStruct_.GatewayWorkerId, 
                socketStruct_.SchedulerId);

            Byte* socketDataBegin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            if (Session != null)
            {
                (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = Session.InternalSession.session_struct_;

                // Updating last active date.
                Session.InternalSession.UpdateLastActive();
            }
            else
            {
                (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);
            }

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = 
                (Byte)MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS;

            (*(UInt32*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = socketStruct_.SocketIndexNum;
            (*(UInt64*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = socketStruct_.SocketUniqueId;
            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = socketStruct_.GatewayWorkerId;

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            // Checking if we have text or binary WebSocket frame.
            if (isText) {
                (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) =
                    (Byte)MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT;
            } else {
                (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = 
                    (Byte)MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY;
            }

            dataStream.SendResponse(data, 0, dataLen, connFlags);
        }
    }
}