// ***********************************************************************
// <copyright file="GatewayHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Advanced;
using System.Diagnostics;
using Starcounter.Rest;
using System.Net;

namespace Starcounter
{
    /// <summary>
    /// Raw socket delegate.
    /// </summary>
    /// <param name="rawSocket"></param>
    /// <param name="incomingData"></param>
    public delegate void RawSocketCallback(
		RawSocket rawSocket,
        Byte[] incomingData
	);

    /// <summary>
    /// UDP socket delegate.
    /// </summary>
    /// <param name="clientIp">IP address of the client.</param>
    /// <param name="datagram">Incoming UDP datagram.</param>
    public delegate void UdpSocketCallback(
        IPAddress clientIp,
        UInt16 clientPort,
        Byte[] datagram
    );

    /// <summary>
    /// Class GatewayHandlers
    /// </summary>
	public unsafe class GatewayHandlers
	{
        /// <summary>
        /// Maximum number of user handlers to register.
        /// </summary>
        const Int32 MAX_HANDLERS = 1024;

        /// <summary>
        /// Raw socket handlers.
        /// </summary>
        private static RawSocketCallback[] rawSocketHandlers_;

        /// <summary>
        /// Number of registered raw port handlers.
        /// </summary>
        static UInt16 numRawPortHandlers_ = 0;

        /// <summary>
        /// UDP socket handlers.
        /// </summary>
        private static UdpSocketCallback[] udpSocketHandlers_;

        /// <summary>
        /// Number of registered UDP port handlers.
        /// </summary>
        static UInt16 numUdpPortHandlers_ = 0;

        /// <summary>
        /// Initializes static members of the <see cref="GatewayHandlers" /> class.
        /// </summary>
        static GatewayHandlers()
		{
            rawSocketHandlers_ = new RawSocketCallback[MAX_HANDLERS];
            udpSocketHandlers_ = new UdpSocketCallback[MAX_HANDLERS];
		}

        /// <summary>
        /// UDP outer handler.
        /// </summary>
        private unsafe static UInt32 HandleUdpSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled) {

            UInt32 errorCode;
            UInt32 chunkIndex = taskInfo->chunk_index;

            try {

                *isHandled = false;

                // Fetching the callback.
                UdpSocketCallback userCallback = udpSocketHandlers_[managedHandlerId];
                if (userCallback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                // Determining if chunk is single.
                Boolean isSingleChunk = ((taskInfo->flags & 0x01) == 0);

                Byte[] dataBytes = new Byte[*(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES)];

                // Checking if we need to process linked chunks.
                if (!isSingleChunk) {

                    fixed (Byte* fixedBuf = dataBytes) {

                        // Copying all chunks data.
                        errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                            chunkIndex,
                            rawChunk,
                            fixedBuf,
                            dataBytes.Length);

                        if (errorCode != 0)
                            throw ErrorCode.ToException(errorCode);
                    }

                } else {

                    // Copying single chunk data into managed buffer.
                    Marshal.Copy(new IntPtr(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)), dataBytes, 0, dataBytes.Length);
                }

                // Getting client IP.
                UInt32 clientIpInt = *(UInt32*) (rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_IP);

                // Obtaining client's IP address.
                IPAddress clientIp = new IPAddress(clientIpInt);

                // Obtaining client's port.
                UInt16 clientPort = *(UInt16*) (rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_UDP_DESTINATION_PORT);

                // Calling user callback.
                userCallback(clientIp, clientPort, dataBytes);

                *isHandled = true;

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();

            } finally {

                // Need to return all chunks here.
                UInt32 err = bmx.sc_bmx_release_linked_chunks(chunkIndex);
                Debug.Assert(0 == err);
                
            }

            return 0;
        }

        /// <summary>
        /// Ports the outer handler.
        /// </summary>
        /// <param name="session_id">The session_id.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="task_info">The task_info.</param>
        /// <param name="is_handled">The is_handled.</param>
        /// <returns>UInt32.</returns>
        private unsafe static UInt32 HandleRawSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
		{
            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            try {

                *isHandled = false;

                UInt32 chunkIndex = taskInfo->chunk_index;

                // Fetching the callback.
                RawSocketCallback userCallback = rawSocketHandlers_[managedHandlerId];
                if (userCallback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                // Determining if chunk is single.
                isSingleChunk = ((taskInfo->flags & 0x01) == 0);

                NetworkDataStream dataStream = new NetworkDataStream(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id);

                // Checking if we need to process linked chunks.
                if (!isSingleChunk) {

                    UInt16 num_chunks = *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = num_chunks * MixedCodeConstants.SHM_CHUNK_SIZE;
                    plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunkIndex,
                        rawChunk,
                        plainRawPtr,
                        totalBytes);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Adjusting pointers to a new plain byte array.
                    rawChunk = plainRawPtr;
                }

                SchedulerResources.SocketContainer sc = SchedulerResources.ObtainSocketContainerForRawSocket(dataStream);

                // Checking if socket exists and legal.
                if (null == sc) {

                    dataStream.Destroy(true);
                    return 0;
                }

                RawSocket rawSocket = sc.Rs;
                Debug.Assert(null != rawSocket);

                Byte[] dataBytes = null;

                // Checking if its a socket disconnect.
                if (((*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_JUST_PUSH_DISCONNECT) == 0) {

                    dataBytes = new Byte[*(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES)];

                    Marshal.Copy(new IntPtr(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)), dataBytes, 0, dataBytes.Length);

                } else {

                    // Making socket unusable.
                    rawSocket.Destroy();
                }

                // Calling user callback.
                userCallback(rawSocket, dataBytes);

                // Destroying original chunk etc.
                rawSocket.DestroyDataStream();

                *isHandled = true;

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();

            } finally {

                // Cleaning the linear buffer in case of multiple chunks.
                if (!isSingleChunk) {

                    BitsAndBytes.Free(plainChunksData);
                    plainChunksData = IntPtr.Zero;
                    rawChunk = null;
                }
            }

			return 0;
		}

        /// <summary>
        /// This is the main entry point of incoming HTTP requests.
        /// It is called from the Gateway via the shared memory IPC (interprocess communication).
        /// </summary>
        internal unsafe static UInt32 HandleHttpRequest(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
        {
            Boolean isSingleChunk = false;

            try
            {
                *isHandled = false;

                UInt32 chunkIndex = taskInfo->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                isSingleChunk = ((taskInfo->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                // Socket data begin.
                Byte* socketDataBegin = rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

                // Checking if we are accumulating on host.
                if (((*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION) != 0)
                {

                }

                // Getting aggregation flag.
                Boolean isAggregated = (((*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_AGGREGATED) != 0);

                // Checking if flag to upgrade to WebSockets is set.
                Boolean wsUpgradeRequest = (((*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_FLAGS_UPGRADE_REQUEST) != 0);

                Request httpRequest = null;

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
                    // Creating network data stream object.
                    NetworkDataStream dataStream = new NetworkDataStream(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id);

                    UInt16 numChunks = *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = numChunks * MixedCodeConstants.SHM_CHUNK_SIZE;
                    IntPtr plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunkIndex,
                        rawChunk,
                        plainRawPtr,
                        totalBytes);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Obtaining Request structure.
                    httpRequest = new Request(
                        rawChunk,
                        isSingleChunk,
                        chunkIndex,
                        managedHandlerId,
                        plainRawPtr + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        plainRawPtr + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA,
                        plainChunksData,
                        dataStream,
                        wsUpgradeRequest,
                        isAggregated);
                }
                else
                {
                    // Creating network data stream object.
                    NetworkDataStream dataStream = new NetworkDataStream(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id);

                    /*if (isAggregated) {
                        data_stream.SendResponse(AggrRespBytes, 0, AggrRespBytes.Length, Response.ConnectionFlags.NoSpecialFlags);

                        return 0;
                    }*/

                    // Obtaining Request structure.
                    httpRequest = new Request(
                        rawChunk,
                        isSingleChunk,
                        taskInfo->chunk_index,
                        managedHandlerId,
                        socketDataBegin + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        socketDataBegin,
                        IntPtr.Zero,
                        dataStream,
                        wsUpgradeRequest,
                        isAggregated);
                }

                // Calling user callback.
                *isHandled = UriInjectMethods.OnHttpMessageRoot_(httpRequest);
            
                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();

            } catch (Exception exc) {

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Clearing current session.
                Session.End();
            }

            return 0;
        }

        internal static void RegisterUriHandlerNative(
            UInt16 port,
            String appName,
            String originalUriInfo,
            String processedUriInfo,
            Byte[] nativeParamTypes,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo) {

            Byte numParams = 0;
            if (null != nativeParamTypes)
                numParams = (Byte)nativeParamTypes.Length;

            unsafe {
                fixed (Byte* pp = nativeParamTypes) {

                    bmx.BMX_HANDLER_CALLBACK fp = HandleHttpRequest;
                    GCHandle gch = GCHandle.Alloc(fp);
                    IntPtr pinned_delegate = Marshal.GetFunctionPointerForDelegate(fp);

                    UInt32 errorCode = bmx.sc_bmx_register_uri_handler(
                        port,
                        appName,
                        originalUriInfo,
                        processedUriInfo,
                        pp,
                        numParams,
                        pinned_delegate,
                        managedHandlerIndex,
                        out handlerInfo);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode, "URI string: " + originalUriInfo);
                }
            }

            String dbName = StarcounterEnvironment.DatabaseNameLower;

            String uriHandlerInfo =
                dbName + " " +
                appName + " " +
                handlerInfo + " " +
                port + " " +
                originalUriInfo.Replace(' ', '\\') + " " +
                processedUriInfo.Replace(' ', '\\') + " " +
                nativeParamTypes.Length;

            String t = "";
            if (nativeParamTypes.Length == 0) {
                t = " 0";
            } else {
                for (Int32 i = 0; i < nativeParamTypes.Length; i++) {
                    t += " " + nativeParamTypes[i];
                }
            }

            uriHandlerInfo += t + "\r\n\r\n\r\n\r\n";

            Byte[] uriHandlerInfoBytes = ASCIIEncoding.ASCII.GetBytes(uriHandlerInfo);

            Response r = Node.LocalhostSystemPortNode.POST("/gw/handler/uri", uriHandlerInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

            if (!r.IsSuccessStatusCode) {

                String errCodeStr = r[MixedCodeConstants.ScErrorCodeHttpHeader];

                if (null != errCodeStr)
                    throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                else
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
            }
        }

        void UnregisterUriHandler(UInt16 port, String originalUriInfo) {

            // Ensuring correct multi-threading handlers creation.
            UInt32 errorCode = bmx.sc_bmx_unregister_uri(port, originalUriInfo);

            if (errorCode != 0)
                throw ErrorCode.ToException(errorCode, "URI string: " + originalUriInfo);
        }

        /// <summary>
        /// This is the main entry point of incoming WebSocket requests.
        /// It is called from the Gateway via the shared memory IPC (interprocess communication).
        /// </summary>
        internal unsafe static UInt32 HandleWebSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
        {
            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            try
            {
                *isHandled = false;

                UInt32 chunkIndex = taskInfo->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                isSingleChunk = ((taskInfo->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                MixedCodeConstants.WebSocketDataTypes wsType = 
                    (MixedCodeConstants.WebSocketDataTypes) (*(Byte*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE));

                WebSocket ws = null;

                // Creating network data stream object.
                NetworkDataStream dataStream = new NetworkDataStream(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id);

                SchedulerResources.SocketContainer sc = SchedulerResources.ObtainSocketContainerForWebSocket(dataStream);

                // Checking if WebSocket exists and legal.
                if (sc == null) {
                    dataStream.Destroy(true);
                    return 0;
                }

                WebSocketInternal wsInternal = sc.Ws;
                Debug.Assert(null != wsInternal);
                Debug.Assert(null != wsInternal.SocketContainer);

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
                    UInt16 numChunks = *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = numChunks * MixedCodeConstants.SHM_CHUNK_SIZE;
                    plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunkIndex,
                        rawChunk,
                        plainRawPtr,
                        totalBytes);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Adjusting pointers to a new plain byte array.
                    rawChunk = plainRawPtr;
                }

                switch (wsType)
                {
                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY:
                    {
                        Byte[] dataBytes = new Byte[*(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_LEN)];

                        Marshal.Copy(new IntPtr(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD)), dataBytes, 0, dataBytes.Length);

                        ws = new WebSocket(wsInternal, null, dataBytes, false, WebSocket.WsHandlerType.BinaryData);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT:
                    {
                        String dataString = new String(
                            (SByte*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD)),
                            0,
                            *(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_LEN),
                            Encoding.UTF8);

                        ws = new WebSocket(wsInternal, dataString, null, true, WebSocket.WsHandlerType.StringMessage);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_CLOSE:
                    {
                        ws = new WebSocket(wsInternal, null, null, false, WebSocket.WsHandlerType.Disconnect);

                        break;
                    }

                    default:
                        throw new Exception("Unknown WebSocket frame type: " + wsType);
                }

                ScSessionStruct sessionStruct = 
                    *(ScSessionStruct*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);

                // Obtaining corresponding Apps session.
                IAppsSession appsSession = GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref sessionStruct);

                // Searching the existing session.
                ws.Session = appsSession;

                // Starting session.
                if (appsSession != null)
                {
                    Session session = (Session)appsSession;
                    session.ActiveWebsocket = ws;
                    Session.Start(session);
                }

                // Setting statically available current WebSocket.
                WebSocket.Current = ws;

                Debug.Assert(null != wsInternal.SocketContainer);

                // Adding session reference.
                *isHandled = AllWsChannels.WsManager.RunHandler(managedHandlerId, ws);

                // Destroying original chunk etc.
                ws.WsInternal.DestroyDataStream();
            
                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();

            } catch (Exception exc) {

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Cleaning the linear buffer in case of multiple chunks.
                if (!isSingleChunk) {

                    BitsAndBytes.Free(plainChunksData);
                    plainChunksData = IntPtr.Zero;
                    rawChunk = null;
                }

                // Clearing current session.
                Session.End();
            }

            return 0;
        }

        /// <summary>
        /// Registers the WebSocket handler.
        /// </summary>
        internal static void RegisterWsChannelHandlerNative(
            UInt16 port,
            String appName,
            String channelName,
            UInt32 channelId,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
        {
            // Ensuring correct multi-threading handlers creation.
            unsafe
            {
                bmx.BMX_HANDLER_CALLBACK fp = HandleWebSocket;
                GCHandle gch = GCHandle.Alloc(fp);
                IntPtr pinnedDelegate = Marshal.GetFunctionPointerForDelegate(fp);

                UInt32 errorCode = bmx.sc_bmx_register_ws_handler(
                    port,
                    appName,
                    channelName,
                    channelId,
                    pinnedDelegate,
                    managedHandlerIndex,
                    out handlerInfo);

                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Channel string: " + channelName);

                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String uriHandlerInfo =
                    dbName + " " +
                    appName + " " +
                    handlerInfo + " " +
                    port + " " +                            
                    channelId + " " +
                    channelName + " ";

                uriHandlerInfo += "\r\n\r\n\r\n\r\n";

                Byte[] uriHandlerInfoBytes = ASCIIEncoding.ASCII.GetBytes(uriHandlerInfo);

                Response r = Node.LocalhostSystemPortNode.POST("/gw/handler/ws", uriHandlerInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r[MixedCodeConstants.ScErrorCodeHttpHeader];

                    if (null != errCodeStr)
                        throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                    else
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
                }
            }
        }

        /// <summary>
        /// Registers UDP port handler.
        /// </summary>
        public static void RegisterUdpPortHandler(
			UInt16 port,
            String appName,
			UdpSocketCallback udpCallback,
            out UInt64 handlerInfo)
		{
            RegisterPortHandler(port, appName, null, udpCallback, out handlerInfo);
        }

        /// <summary>
        /// Registers TCP port handler.
        /// </summary>
        public static void RegisterTcpPortHandler(
			UInt16 port,
            String appName,
			RawSocketCallback rawCallback,
            out UInt64 handlerInfo)
		{
            RegisterPortHandler(port, appName, rawCallback, null, out handlerInfo);
		}

        /// <summary>
        /// Registering port handler.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="appName"></param>
        /// <param name="rawCallback"></param>
        /// <param name="udpCallback"></param>
        /// <param name="handlerInfo"></param>
        static void RegisterPortHandler(
			UInt16 port,
            String appName,
			RawSocketCallback rawCallback,
            UdpSocketCallback udpCallback,
            out UInt64 handlerInfo) {

            Boolean isUdp = false;
            if (udpCallback != null)
                isUdp = true;

            // Ensuring correct multi-threading handlers creation.
            lock (rawSocketHandlers_) {

                bmx.BMX_HANDLER_CALLBACK fp = null;

                if (udpCallback == null)
                    fp = HandleRawSocket;
                else
                    fp = HandleUdpSocket;

                GCHandle gch = GCHandle.Alloc(fp);
                IntPtr pinnedDelegate = Marshal.GetFunctionPointerForDelegate(fp);

                UInt16 numHandlers = numRawPortHandlers_;
                if (udpCallback != null)
                    numHandlers = numUdpPortHandlers_;

                UInt32 errorCode = bmx.sc_bmx_register_port_handler(port, appName, pinnedDelegate, numHandlers, out handlerInfo);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port);

                if (udpCallback == null) {
                    rawSocketHandlers_[numRawPortHandlers_] = rawCallback;
                    numRawPortHandlers_++;
                } else {
                    udpSocketHandlers_[numUdpPortHandlers_] = udpCallback;
                    numUdpPortHandlers_++;
                }

                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String portInfo =
                    dbName + " " +
                    appName + " " +
                    handlerInfo + " " +
                    port + " " +
                    isUdp.ToString().ToLowerInvariant() + " ";

                portInfo += "\r\n\r\n\r\n\r\n";

                Byte[] portInfoBytes = ASCIIEncoding.ASCII.GetBytes(portInfo);

                Response r = Node.LocalhostSystemPortNode.POST("/gw/handler/port", portInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r[MixedCodeConstants.ScErrorCodeHttpHeader];

                    if (null != errCodeStr)
                        throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                    else
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
                }
            }
        }

        public static void UnregisterPort(UInt16 port, UInt64 handlerInfo)
		{
            // Ensuring correct multi-threading handlers creation.
            lock (rawSocketHandlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_unregister_port(port);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port);

                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String portInfo =
                    dbName + " " +
                    handlerInfo + " " +
                    port + " ";

                portInfo += "\r\n\r\n\r\n\r\n";

                Byte[] portInfoBytes = ASCIIEncoding.ASCII.GetBytes(portInfo);

                Response r = Node.LocalhostSystemPortNode.DELETE("/gw/handler/port", portInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r[MixedCodeConstants.ScErrorCodeHttpHeader];

                    if (null != errCodeStr)
                        throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                    else
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
                }
            }
		}
	}
}
