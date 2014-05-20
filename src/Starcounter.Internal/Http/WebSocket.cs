using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Starcounter
{
    internal class WebSocketInternal
    {
        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        UInt64 socketUniqueId_;

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        internal UInt64 SocketUniqueId { get { return socketUniqueId_; } }

        /// <summary>
        /// User cargo id.
        /// </summary>
        UInt64 cargoId_;

        /// <summary>
        /// Cargo ID getter.
        /// </summary>
        internal UInt64 CargoId { get { return cargoId_; } } 

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        UInt32 socketIndexNum_;

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        internal UInt32 SocketIndexNum { get { return socketIndexNum_; } }

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        Byte gatewayWorkerId_;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte GatewayWorkerId { get { return gatewayWorkerId_; } }

        /// <summary>
        /// Corresponding channel ID.
        /// </summary>
        UInt32 channelId_;

        /// <summary>
        /// Channel ID getter.
        /// </summary>
        internal UInt32 ChannelId { get { return channelId_; } }

        /// <summary>
        /// Active WebSocket node.
        /// </summary>
        LinkedListNode<UInt32> activeWebSocketNode_;

        /// <summary>
        /// Active WebSocket node.
        /// </summary>
        internal LinkedListNode<UInt32> ActiveWebSocketNode {
            get { return activeWebSocketNode_; }
            set { activeWebSocketNode_ = value; }
        }

        internal WebSocketInternal()
        {
            Reset();
        }

        internal void Init(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            UInt64 cargoId,
            UInt32 channelId)
        {
            socketIndexNum_ = socketIndexNum;
            socketUniqueId_ = socketUniqueId;
            gatewayWorkerId_ = gatewayWorkerId;
            cargoId_ = cargoId;
            channelId_ = channelId;
        }

        internal void Reset()
        {
            gatewayWorkerId_ = 255;
            activeWebSocketNode_ = null;
        }

        public Boolean IsDead()
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
                return wsInternal_.CargoId;
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

            Destroy();
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

        /// <summary>
        /// Running the given action on WebSockets that meet given criteria (channel name and cargo, if any).
        /// </summary>
        /// <param name="action">Given action that should be performed with each WebSocket.</param>
        public static void ForEach(Action<WebSocket> action) {
            ForEach(null, UInt64.MaxValue, action);
        }

        /// <summary>
        /// Running the given action on WebSockets that meet given criteria (channel name and cargo, if any).
        /// </summary>
        /// <param name="action">Given action that should be performed with each WebSocket.</param>
        /// <param name="channelName">Channel name filter for WebSockets.</param>
        public static void ForEach(String channelName, Action<WebSocket> action) {
            ForEach(channelName, UInt64.MaxValue, action);
        }

        /// <summary>
        /// Running the given action on WebSockets that meet given criteria (channel name and cargo, if any).
        /// </summary>
        /// <param name="action">Given action that should be performed with each WebSocket.</param>
        /// <param name="cargoId">Cargo ID filter.</param>
        public static void ForEach(UInt64 cargoId, Action<WebSocket> action) {
            ForEach(null, cargoId, action);
        }

        /// <summary>
        /// Running the given action on WebSockets that meet given criteria (channel name and cargo, if any).
        /// </summary>
        /// <param name="action">Given action that should be performed with each WebSocket.</param>
        /// <param name="channelName">Channel name filter for WebSockets.</param>
        /// <param name="cargoId">Cargo ID filter.</param>
        public static void ForEach(String channelName, UInt64 cargoId, Action<WebSocket> action) {

            UInt32 channelId = 0;
            if (channelName != null)
                channelId = WsChannelInfo.CalculateChannelIdFromChannelName(channelName);

            // For each scheduler.
            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                Byte schedId = i;

                // Running asynchronous task.
                ScSessionClass.DbSession.RunAsync(() => {

                    // Saving current WebSocket since we are going to set other.
                    WebSocket origCurrentWebSocket = WebSocket.Current;

                    try
                    {
                        // Number of processed WebSockets per scheduler.
                        SchedulerWebSockets sws = AllWebSockets.GetSchedulerWebSockets(schedId);

                        // Going through each active WebSocket.
                        foreach (UInt32 wsIndex in sws.ActiveWebSocketIndexes) {

                            // Getting internal WebSocket structure.
                            WebSocketInternal wsInternal = sws.GetWebSocketInternal(wsIndex);

                            // Checking if WebSocket is alive.
                            if (!wsInternal.IsDead()) {

                                // Comparing given channel name if any.
                                if ((channelName == null) || (wsInternal.ChannelId == channelId)) {

                                    // Comparing given cargo ID if any.
                                    if ((cargoId == UInt64.MaxValue) || (cargoId == wsInternal.CargoId)) {

                                        // Creating WebSocket object used for pushes.
                                        WebSocket ws = new WebSocket(wsInternal, null, null, null, WebSocket.WsHandlerType.Empty);

                                        // Setting current WebSocket.
                                        WebSocket.Current = ws;

                                        // Running user delegate with WebSocket as parameter.
                                        action(ws);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Restoring original current WebSocket.
                        WebSocket.Current = origCurrentWebSocket;
                    }

                }, schedId);
            }
        }

        /// <summary>
        /// Disconnecting WebSockets that meet given criteria.
        /// </summary>
        public static void DisconnectEach(String channelName = null, UInt64 cargoId = UInt64.MaxValue) {
            ForEach(channelName, cargoId, (WebSocket ws) => { ws.Disconnect(); });
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

                Byte schedId = StarcounterEnvironment.CurrentSchedulerId;
                SchedulerWebSockets sws = AllWebSockets.GetSchedulerWebSockets(schedId);

                WebSocketInternal ws = sws.GetWebSocketInternal(socketIndex);

                // Checking that WebSocket is correct (comparing unique indexes).
                if ((ws == null) || (ws.SocketUniqueId != uniqueId))
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
            UInt64 cargoId,
            UInt32 channelId)
        {
            Byte schedId = StarcounterEnvironment.CurrentSchedulerId;
            SchedulerWebSockets sws = AllWebSockets.GetSchedulerWebSockets(schedId);

            WebSocketInternal wsi = sws.AddNewWebSocket(socketIndexNum);

            wsi.Init(
                socketIndexNum,
                socketUniqueId,
                gatewayWorkerId,
                cargoId,
                channelId);

            wsInternal_ = wsi;
        }

        internal void Destroy() {
            Byte schedId = StarcounterEnvironment.CurrentSchedulerId;

            SchedulerWebSockets sws = AllWebSockets.GetSchedulerWebSockets(schedId);
            sws.RemoveActiveWebSocket(wsInternal_);

            wsInternal_.Reset();
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
                data_stream = new NetworkDataStream(chunk_mem, chunk_index, ws.wsInternal_.GatewayWorkerId);
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
            else
            {
                (*(ScSessionStruct*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);
            }

            (*(UInt32*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte)MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS;

            (*(UInt32*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = ws.wsInternal_.SocketIndexNum;
            (*(UInt64*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = ws.wsInternal_.SocketUniqueId;
            (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = ws.wsInternal_.GatewayWorkerId;

            (*(UInt16*)(chunk_mem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            // Checking if we have text or binary WebSocket frame.
            if (isText)
                (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT;
            else
                (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY;

            data_stream.SendResponse(data, 0, dataLen, connFlags);
        }

        internal class GlobalWebSockets
        {
            static SchedulerWebSockets[] WebSocketsPerScheduler = null;

            internal SchedulerWebSockets GetSchedulerWebSockets(Byte schedId) {
                return WebSocketsPerScheduler[schedId];
            }

            public GlobalWebSockets() {
                WebSocketsPerScheduler = new SchedulerWebSockets[StarcounterEnvironment.SchedulerCount];

                for (Int32 i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                    WebSocketsPerScheduler[i] = new SchedulerWebSockets();
                }
            }
        }

        internal static GlobalWebSockets AllWebSockets;

        internal class SchedulerWebSockets
        {
            public const Int32 MaxWebSocketsPerScheduler = 30000;

            WebSocketInternal[] webSockets_ = new WebSocketInternal[MaxWebSocketsPerScheduler];
            internal WebSocketInternal[] WebSockets { get { return webSockets_; } }

            LinkedList<UInt32> activeWebSocketIndexes_ = new LinkedList<UInt32>();
            internal LinkedList<UInt32> ActiveWebSocketIndexes { get { return activeWebSocketIndexes_; } }

            LinkedList<UInt32> freeLinkedListNodes_ = new LinkedList<UInt32>();

            internal WebSocketInternal GetWebSocketInternal(UInt32 socketIndexNum) {
                return webSockets_[socketIndexNum];
            }

            internal WebSocketInternal AddNewWebSocket(UInt32 socketIndexNum) {

                if (webSockets_[socketIndexNum] == null) {
                    webSockets_[socketIndexNum] = new WebSocketInternal();
                }

                // TODO: Check what happens with WebSockets occupying same slot and if this can happen at all.
                //Debug.Assert(webSockets_[socketIndexNum].IsDead());

                LinkedListNode<UInt32> lln = null;
                if (freeLinkedListNodes_.First != null) {
                    lln = freeLinkedListNodes_.First;
                    freeLinkedListNodes_.RemoveFirst();
                    lln.Value = socketIndexNum;
                } else {
                    lln = new LinkedListNode<UInt32>(socketIndexNum);
                }

                activeWebSocketIndexes_.AddLast(lln);

                // Adding reference from WebSocket to the active linked list node.
                webSockets_[socketIndexNum].ActiveWebSocketNode = lln;

                return webSockets_[socketIndexNum];
            }

            internal void RemoveActiveWebSocket(WebSocketInternal wsi) {
                activeWebSocketIndexes_.Remove(wsi.ActiveWebSocketNode);
                freeLinkedListNodes_.AddLast(wsi.ActiveWebSocketNode);
                wsi.ActiveWebSocketNode = null;
            }
        }

        internal static void InitWebSocketsInternal()
        {
            AllWebSockets = new GlobalWebSockets();
        }
    }
}