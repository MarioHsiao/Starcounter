using Starcounter;
using Starcounter.Advanced;
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
        /// Corresponding channel ID.
        /// </summary>
        UInt32 channelId_;

        /// <summary>
        /// Channel ID getter.
        /// </summary>
        internal UInt32 ChannelId { get { return channelId_; } }

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        public UInt64 SocketUniqueId { get { return socketContainer_.SocketUniqueId; } }

        /// <summary>
        /// Socket id on gateway.
        /// </summary>
        public UInt32 SocketIndexNum { get { return socketContainer_.SocketIndexNum; } }

        /// <summary>
        /// Cargo ID getter.
        /// </summary>
        public UInt64 CargoId {
            get { return socketContainer_.CargoId; }
            set { socketContainer_.CargoId = value; }
        }

        /// <summary>
        /// Network data stream.
        /// </summary>
        public NetworkDataStream DataStream { get { return socketContainer_.DataStream; } }

        /// <summary>
        /// Socket container.
        /// </summary>
        SchedulerResources.SocketContainer socketContainer_;

        internal SchedulerResources.SocketContainer SocketContainer {
            get { return socketContainer_; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="outerSocketContainer"></param>
        /// <param name="channelId"></param>
        public WebSocketInternal(SchedulerResources.SocketContainer outerSocketContainer, UInt32 channelId)
        {
            socketContainer_ = outerSocketContainer;
            channelId_ = channelId;
        }

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        public Byte GatewayWorkerId { get { return socketContainer_.GatewayWorkerId; } }

        /// <summary>
        /// Destroys only data stream.
        /// </summary>
        /// <param name="isStarcounterThread"></param>
        public void DestroyDataStream() {
            if (null != socketContainer_)
                socketContainer_.DestroyDataStream();
        }

        /// <summary>
        /// Resets the socket.
        /// </summary>
        public void Destroy() {
            if (null != socketContainer_) {
                SchedulerResources.ReturnSocketContainer(socketContainer_);
                socketContainer_ = null;
                channelId_ = MixedCodeConstants.INVALID_WS_CHANNEL_ID;
            }
        }

        /// <summary>
        /// Checks if socket is dead.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDead() {
            return ((null == socketContainer_) || (socketContainer_.IsDead()));
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

        WebSocketInternal wsInternal_;

        internal WebSocketInternal WsInternal { get { return wsInternal_; } }

        /// <summary>
        /// Network data stream.
        /// </summary>
        public NetworkDataStream DataStream {
            get {
                Debug.Assert(null != wsInternal_);

                return wsInternal_.DataStream;
            }
        }

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
        /// <param name="dataLen">Length of data in bytes.</param>
        /// <param name="isText">Is given data a text?</param>
        /// <param name="connFlags">Connection flags on the push.</param>
        public void Send(Byte[] data, Int32 dataLen, Boolean isText = false, Response.ConnectionFlags connFlags = Response.ConnectionFlags.NoSpecialFlags) {
            PushServerMessage(this, data, dataLen, isText, connFlags);
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

            UInt32 channelId = MixedCodeConstants.INVALID_WS_CHANNEL_ID;

            if (channelName != null)
                channelId = WsChannelInfo.CalculateChannelIdFromChannelName(channelName);

            // For each scheduler.
            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                Byte schedId = i;

                // Running asynchronous task.
                ScSessionClass.DbSession.RunAsync(() => {

                    // Saving current WebSocket since we are going to set other.
                    WebSocket origCurrentWebSocket = WebSocket.Current;

                    try {
                        SchedulerResources.SchedulerSockets ss = SchedulerResources.AllHostSockets.GetSchedulerSockets(schedId);

                        // Going through each gateway worker.
                        for (Byte gwWorkerId = 0; gwWorkerId < StarcounterEnvironment.Gateway.NumberOfWorkers; gwWorkerId++) {

                            SchedulerResources.SocketsPerSchedulerPerGatewayWorker spspgw = ss.GetSocketsPerGatewayWorker(gwWorkerId);

                            // Going through each active socket.
                            foreach (UInt32 wsIndex in spspgw.ActiveSocketIndexes) {

                                // Getting socket container.
                                SchedulerResources.SocketContainer sc = spspgw.GetSocket(wsIndex);

                                // Checking if socket is alive.
                                if ((sc != null) && (!sc.IsDead())) {

                                    WebSocketInternal wsInternal = sc.Ws;

                                    // Checking if its WebSocket.
                                    if (null != wsInternal) {

                                        // Comparing given channel name if any.
                                        if ((null == channelName) || (wsInternal.ChannelId == channelId)) {

                                            // Comparing given cargo ID if any.
                                            if ((cargoId == UInt64.MaxValue) || (cargoId == wsInternal.CargoId)) {

                                                // Creating WebSocket object used for pushes.
                                                WebSocket ws = new WebSocket(wsInternal, null, null, false, WebSocket.WsHandlerType.Empty);

                                                // Setting current WebSocket.
                                                WebSocket.Current = ws;

                                                // Running user delegate with WebSocket as parameter.
                                                action(ws);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } finally {
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
        internal String Message
        {
            get {
                return message_;
            }
        }

        Byte[] bytes_;

        /// <summary>
        /// Received binary bytes.
        /// </summary>
        internal Byte[] Bytes
        {
            get {
                return bytes_;
            }
        }

        WsHandlerType wsHandlerType_;

        internal WsHandlerType HandlerType
        {
            get { return wsHandlerType_; }
        }

        internal WebSocket(
            WebSocketInternal wsInternal,
            String message,
            Byte[] bytes,
            Boolean isText,
            WsHandlerType wsHandlerType)
        {
            wsInternal_ = wsInternal;
            message_ = message;
            bytes_ = bytes;
            wsHandlerType_ = wsHandlerType;
            isText_ = isText;
        }

        internal void ConstructFromRequest(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId,
            UInt64 cargoId,
            UInt32 channelId)
        {
            WebSocketInternal wsi = SchedulerResources.CreateNewWebSocket(socketIndexNum, socketUniqueId, gatewayWorkerId, cargoId, channelId);

            wsInternal_ = wsi;
        }

        /// <summary>
        /// Destroys the socket.
        /// </summary>
        /// <param name="isStarcounterThread"></param>
        internal void Destroy() {

            Debug.Assert(null != wsInternal_);

            wsInternal_.Destroy();
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
            NetworkDataStream dataStream;
            UInt32 chunkIndex;
            Byte* chunkMem;

            // Checking if we still have the data stream with original chunk available.
            if (ws.DataStream == null || ws.DataStream.IsDestroyed())
            {
                UInt32 err_code = bmx.sc_bmx_obtain_new_chunk(&chunkIndex, &chunkMem);
                if (0 != err_code) {
                    throw ErrorCode.ToException(err_code, "Can't obtain new chunk for session push.");
                }

                // Creating network data stream object.
                System.Diagnostics.Debug.Assert(ws.wsInternal_ != null);
                dataStream = new NetworkDataStream();
                dataStream.Init(chunkMem, chunkIndex, ws.wsInternal_.GatewayWorkerId, false);
            }
            else
            {
                dataStream = ws.DataStream;
                chunkIndex = dataStream.ChunkIndex;
                chunkMem = dataStream.RawChunk;
            }

            Byte* socketDataBegin = chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

            if (ws.Session != null)
            {
                (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = ws.Session.InternalSession.session_struct_;

                // Updating last active date.
                ws.Session.InternalSession.UpdateLastActive();
            }
            else
            {
                (*(ScSessionStruct*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SESSION)) = new ScSessionStruct(true);
            }

            (*(UInt32*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) = 0;

            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE)) = (Byte)MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS;

            (*(UInt32*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER)) = ws.wsInternal_.SocketIndexNum;
            (*(UInt64*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID)) = ws.wsInternal_.SocketUniqueId;
            (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_BOUND_WORKER_ID)) = ws.wsInternal_.GatewayWorkerId;

            (*(UInt16*)(chunkMem + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)) =
                MixedCodeConstants.SOCKET_DATA_OFFSET_BLOB;

            // Checking if we have text or binary WebSocket frame.
            if (isText)
                (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT;
            else
                (*(Byte*)(socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE)) = (Byte) MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY;

            dataStream.SendResponse(data, 0, dataLen, connFlags);
        }
    }
}