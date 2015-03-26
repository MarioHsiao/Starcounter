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
        private static Action<TcpSocket, Byte[]>[] rawSocketHandlers_;

        /// <summary>
        /// Number of registered raw port handlers.
        /// </summary>
        static UInt16 numRawPortHandlers_ = 0;

        /// <summary>
        /// UDP socket handlers.
        /// </summary>
        private static Action<IPAddress, UInt16, Byte[]>[] udpSocketHandlers_;

        /// <summary>
        /// Number of registered UDP port handlers.
        /// </summary>
        static UInt16 numUdpPortHandlers_ = 0;

        /// <summary>
        /// Initializes static members of the <see cref="GatewayHandlers" /> class.
        /// </summary>
        static GatewayHandlers()
		{
            rawSocketHandlers_ = new Action<TcpSocket, Byte[]>[MAX_HANDLERS];
            udpSocketHandlers_ = new Action<IPAddress, UInt16, Byte[]>[MAX_HANDLERS];
		}

        /// <summary>
        /// UDP outer handler.
        /// </summary>
        unsafe static UInt32 HandleUdpSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled) {

            UInt32 errorCode;
            UInt32 chunkIndex = taskInfo->chunk_index;

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

            try {
                *isHandled = false;

                // Fetching the callback.
                Action<IPAddress, UInt16, Byte[]> userCallback = udpSocketHandlers_[managedHandlerId];
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

                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                userCallback(clientIp, clientPort, dataBytes);
                *isHandled = true;

            } finally {

                // Need to return all chunks here.
                UInt32 err = bmx.sc_bmx_release_linked_chunks(chunkIndex);
                Debug.Assert(0 == err);

                // Needs to be called before the stackallocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

            return 0;
        }

        /// <summary>
        /// Ports the outer handler.
        /// </summary>
        unsafe static UInt32 HandleRawSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
		{
            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

            try {
                *isHandled = false;

                UInt32 chunkIndex = taskInfo->chunk_index;

                // Fetching the callback.
                Action<TcpSocket, Byte[]> userCallback = rawSocketHandlers_[managedHandlerId];
                if (userCallback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                // Determining if chunk is single.
                isSingleChunk = ((taskInfo->flags & 0x01) == 0);

                NetworkDataStream dataStream = new NetworkDataStream();
                dataStream.Init(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id, true);

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

                TcpSocket rawSocket = sc.Rs;
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

                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                // Calling user callback.
                userCallback(rawSocket, dataBytes);
                
                // Destroying original chunk etc.
                rawSocket.DestroyDataStream();

                *isHandled = true;

            } finally {

                // Cleaning the linear buffer in case of multiple chunks.
                if (!isSingleChunk) {

                    BitsAndBytes.Free(plainChunksData);
                    plainChunksData = IntPtr.Zero;
                    rawChunk = null;
                }

                // Needs to be called before the stackallocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

			return 0;
		}

        /// <summary>
        /// This is the main entry point of incoming HTTP requests.
        /// It is called from the Gateway via the shared memory IPC (interprocess communication).
        /// </summary>
        unsafe static UInt32 HandleHttpRequest(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
        {
            Boolean isSingleChunk = false;

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

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

                SchedulerResources sr = SchedulerResources.Current;
                Request req = new Request();
                NetworkDataStream dataStream = new NetworkDataStream();

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
                    // Creating network data stream object.
                    dataStream.Init(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id, false);

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
                    req.InitExternal(
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
                    dataStream.Init(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id, false);

                    /*if (isAggregated) {
                        data_stream.SendResponse(AggrRespBytes, 0, AggrRespBytes.Length, Response.ConnectionFlags.NoSpecialFlags);

                        return 0;
                    }*/

                    // Obtaining Request structure.
                    req.InitExternal(
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

                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                *isHandled = UriInjectMethods.OnHttpMessageRoot_(req); 
            } catch (Exception exc) {

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Clearing current session.
                Session.End();

                // Needs to be called before the stackallocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

            return 0;
        }

        internal  static void RegisterUriHandlerNative(
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
        unsafe static UInt32 HandleWebSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
        {
            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

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
                NetworkDataStream dataStream = new NetworkDataStream();
                dataStream.Init(rawChunk, taskInfo->chunk_index, taskInfo->client_worker_id, true);

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

                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                // Adding session reference.
                *isHandled = AllWsChannels.WsManager.RunHandler(managedHandlerId, ws);
                // Destroying original chunk etc.
                ws.WsInternal.DestroyDataStream();
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

                // Needs to be called before the stackallocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();                

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
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

                Response r = Node.LocalhostSystemPortNode.POST("/gw/handler/ws", uriHandlerInfoBytes, null, 0,
                    new HandlerOptions() { CallExternalOnly = true });

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
        internal static void RegisterUdpSocketHandler(
			UInt16 port,
            String appName,
			Action<IPAddress, UInt16, Byte[]> udpCallback,
            out UInt64 handlerInfo)
		{
            RegisterPortHandler(port, appName, null, udpCallback, out handlerInfo);
        }

        /// <summary>
        /// Registers TCP port handler.
        /// </summary>
        internal static void RegisterTcpSocketHandler(
			UInt16 port,
            String appName,
			Action<TcpSocket, Byte[]> rawCallback,
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
			Action<TcpSocket, Byte[]> rawCallback,
            Action<IPAddress, UInt16, Byte[]> udpCallback,
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

        static void UnregisterPort(UInt16 port, UInt64 handlerInfo)
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
