using Starcounter;
using Starcounter.Internal;
using System;
using System.Net;
using System.Text;

namespace Starcounter
{
    internal class WebSocketInternal
    {
        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        internal UInt64 socketUniqueId_;

        /// <summary>
        /// User cargo id.
        /// </summary>
        internal UInt64 cargoId_;

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        internal UInt32 socketIndexNum_;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte gatewayWorkerId_;

        internal WebSocketInternal()
        {
            Reset();
        }

        internal void Init(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            UInt64 cargoId)
        {
            socketIndexNum_ = socketIndexNum;
            socketUniqueId_ = socketUniqueId;
            gatewayWorkerId_ = gatewayWorkerId;
            cargoId_ = cargoId;
        }

        void Reset()
        {
            gatewayWorkerId_ = 255;
        }

        Boolean IsDead()
        {
            return (gatewayWorkerId_ == 255);
        }
    }

    public class WebSocket
    {
        internal enum WsHandlerType
        {
            StringMessage,
            BinaryData,
            Disconnect,
            Empty
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

        internal WebSocketInternal wsInternal_;

        /// <summary>
        /// Reference to existing session if any.
        /// </summary>
        public IAppsSession Session
        {
            get;
            internal set;
        }

        /// <summary>
        /// Specific saved user object ID.
        /// </summary>
        public UInt64 CargoId
        {
            get
            {
                return wsInternal_.cargoId_;
            }
        }

        /// <summary>
        /// WebSocket close codes.
        /// </summary>
        public enum WebSocketCloseCodes
        {
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
        /// Disconnects active WebSocket.
        /// </summary>
        /// <param name="message">Optional error message.</param>
        /// <param name="code">Optional error code.</param>
        public void Disconnect(String message = null, WebSocketCloseCodes code = WebSocketCloseCodes.WS_CLOSE_NORMAL)
        {
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

            PushServerMessage(this, buf, bytesWritten, true, Response.ConnectionFlags.GracefullyCloseConnection);
        }

        // Executes given user delegate on all active sockets.
        internal static void RunOnAllActiveWebSockets(Action<WebSocket> userDelegate, String channel)
        {

        }

        /// <summary>
        /// Server push on WebSocket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
        public void Send(Byte[] data, Boolean isText = false, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags)
        {
            PushServerMessage(this, data, data.Length, isText, connFlags);
        }

        /// <summary>
        /// Server push on WebSocket.
        /// </summary>
        /// <param name="data">Data to push.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
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

        WsHandlerType wsHandlerType_;

        internal WsHandlerType HandlerType
        {
            get { return wsHandlerType_; }
        }

        internal static WebSocketInternal ObtainWebSocketInternal(NetworkDataStream dataStream)
        {
            unsafe
            {
                // Obtaining socket index and unique id.
                UInt32 socketIndex = *(UInt32*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER);
                UInt64 uniqueId = *(UInt64*)(dataStream.RawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID);

                // Comparing with WebSocket internal belonging to that index.
                WebSocketInternal ws = allWebSockets[StarcounterEnvironment.GetCurrentSchedulerId(), socketIndex];
                if ((ws == null) || (ws.socketUniqueId_ != uniqueId))
                    return null;

                return ws;
            }
        }

        internal WebSocket(WebSocketInternal wsInternal, NetworkDataStream dataStream, String message, Byte[] bytes, WsHandlerType wsHandlerType)
        {
            wsInternal_ = wsInternal;
            dataStream_ = dataStream;
            message_ = message;
            bytes_ = bytes;
            wsHandlerType_ = wsHandlerType;
        }

        internal void ConstructFromRequest(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            UInt64 cargoId)
        {
            Byte schedId = StarcounterEnvironment.GetCurrentSchedulerId();
            if (allWebSockets[schedId, socketIndexNum] == null)
                allWebSockets[schedId, socketIndexNum] = new WebSocketInternal();

            allWebSockets[schedId, socketIndexNum].Init(
                socketIndexNum,
                socketUniqueId,
                gatewayWorkerId,
                cargoId);

            wsInternal_ = allWebSockets[schedId, socketIndexNum];
        }

        internal void ManualDestroy()
        {
            // Releasing data stream resources like chunks, etc.
            if (dataStream_ != null)
                dataStream_.Destroy(true);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        ~WebSocket()
        {
            // Releasing data stream resources like chunks, etc.
            if (dataStream_ != null)
                dataStream_.Destroy(false);
        }
        
        /// <summary>
        /// Creates new Request based on session.
        /// </summary>
        internal unsafe static void PushServerMessage(
            WebSocket ws,
            Byte[] data,
            Int32 dataLen,
            Boolean isText,
            Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags)
        {
            NetworkDataStream data_stream;
            UInt32 chunk_index;
            Byte* chunk_mem;

            // Checking if we still have the data stream with original chunk available.
            if (ws.dataStream_ == null || ws.dataStream_.IsDestroyed())
            {
                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunk_index, &chunk_mem);
                if (0 != err_code)
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");

                // Creating network data stream object.
                System.Diagnostics.Debug.Assert(ws.wsInternal_ != null);
                data_stream = new NetworkDataStream(chunk_mem, chunk_index, ws.wsInternal_.gatewayWorkerId_);
            }
            else
            {
                data_stream = ws.dataStream_;
                chunk_index = data_stream.ChunkIndex;
                chunk_mem = data_stream.RawChunk;
            }

            Byte* socket_data_begin = chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            if (ws.Session != null)
            {
                (*(ScSessionStruct*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = ws.Session.InternalSession.session_struct_;

                // Updating last active date.
                ws.Session.InternalSession.UpdateLastActive();
            }

            (*(UInt32*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte)MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = ws.wsInternal_.socketIndexNum_;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = ws.wsInternal_.socketUniqueId_;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = ws.wsInternal_.gatewayWorkerId_;

            (*(UInt16*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            // Checking if we have text or binary WebSocket frame.
            if (isText)
                (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT;
            else
                (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY;

            data_stream.SendResponse(data, 0, dataLen, connFlags);
        }

        static WebSocketInternal[,] allWebSockets = null;

        internal static void InitWebSocketsInternal()
        {
            allWebSockets = new WebSocketInternal[StarcounterEnvironment.SchedulerCount, 10000];
        }
    }
}