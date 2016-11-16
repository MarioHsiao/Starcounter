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
using Starcounter.Logging;

namespace Starcounter
{
    public enum HandlerTypes {
        NotRegistered,
        TcpHandler,
        UdpHandler,
        WebSocketHandler,
        HttpHandler
    };

    public struct ManagedHandlerInfo {
        public HandlerTypes HandlerType;
        public UInt64 HandlerInfo;
        public UInt16 ManagedHandlerId;

        public void Reset() {

            HandlerInfo = 0;
            ManagedHandlerId = UInt16.MaxValue;
            HandlerType = HandlerTypes.NotRegistered;
        }
    }

    /// <summary>
    /// Class GatewayHandlers
    /// </summary>
	public unsafe class GatewayHandlers
	{
        /// <summary>
        /// All handler types.
        /// </summary>
        static ManagedHandlerInfo[] allHandlers_ = new ManagedHandlerInfo[UInt16.MaxValue];

        /// <summary>
        /// Global unique handlers id.
        /// </summary>
        static UInt64 uniqueHandlersId_ = 1;

        /// <summary>
        /// Handles generic managed handler.
        /// </summary>
        public unsafe static UInt32 HandleManaged(
            UInt64 handlerInfo,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled) {

            try {

                UInt16 handlerIndex = (UInt16)handlerInfo;

                ManagedHandlerInfo m = allHandlers_[handlerIndex];

                // Checking if we are addressing the correct handler.
                if (handlerInfo != m.HandlerInfo) {

                    bmx.sc_bmx_release_linked_chunks(&taskInfo->chunk_index);
                    *isHandled = false;

                    return 0;
                }

                switch (m.HandlerType) {

                    case HandlerTypes.NotRegistered: {
                        bmx.sc_bmx_release_linked_chunks(&taskInfo->chunk_index);
                        *isHandled = false;

                        return 0;
                    }

                    case HandlerTypes.TcpHandler: {
                        return HandleTcpSocket(m.ManagedHandlerId, rawChunk, taskInfo, isHandled);
                    }

                    case HandlerTypes.UdpHandler: {
                        return HandleUdpSocket(m.ManagedHandlerId, rawChunk, taskInfo, isHandled);
                    }

                    case HandlerTypes.WebSocketHandler: {
                        return HandleWebSocket(m.ManagedHandlerId, rawChunk, taskInfo, isHandled);
                    }

                    case HandlerTypes.HttpHandler: {
                        return HandleHttpRequest(m.ManagedHandlerId, rawChunk, taskInfo, isHandled);
                    }
                }

                *isHandled = false;
                return 0;

            } catch (Exception exc) {
                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;
            }
        }

        /// <summary>
        /// Registers generic managed handler.
        /// </summary>
        static void RegisterManagedHandler(HandlerTypes handlerType, UInt16 managedHandlerId, out UInt64 handlerInfo) {

            handlerInfo = 0;

            lock (allHandlers_) {

                for (UInt16 i = 0; i < allHandlers_.Length; i++) {

                    // Searching for the first unoccupied handler.
                    if (HandlerTypes.NotRegistered == allHandlers_[i].HandlerType) {

                        uniqueHandlersId_++;

                        handlerInfo = (((UInt64)uniqueHandlersId_) << 16) | i;

                        allHandlers_[i].HandlerInfo = handlerInfo;
                        allHandlers_[i].ManagedHandlerId = managedHandlerId;
                        allHandlers_[i].HandlerType = handlerType;

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters managed handler.
        /// </summary>
        static void UnregisterManagedHandler(UInt16 handlerId) {

            lock (allHandlers_) {
                allHandlers_[handlerId].Reset();
            }
        }

        /// <summary>
        /// Maximum number of user handlers to register.
        /// </summary>
        const Int32 MAX_HANDLERS = 1024;

        /// <summary>
        /// TCP socket handlers.
        /// </summary>
        private static Action<TcpSocket, Byte[]>[] tcpSocketHandlers_;

        /// <summary>
        /// Number of registered TCP port handlers.
        /// </summary>
        static UInt16 numTcpPortHandlers_ = 0;

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
            tcpSocketHandlers_ = new Action<TcpSocket, Byte[]>[MAX_HANDLERS];
            udpSocketHandlers_ = new Action<IPAddress, UInt16, Byte[]>[MAX_HANDLERS];
		}

        /// <summary>
        /// Contains number of requests for each scheduler.
        /// </summary>
        public static Int64[] SchedulerNumRequests = new Int64[StarcounterConstants.MaximumSchedulersNumber];

        /// <summary>
        /// UDP outer handler.
        /// </summary>
        unsafe static UInt32 HandleUdpSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled) {

            // First marking handler as not handled.
            *isHandled = false;

            // Distribution statistics.
            SchedulerNumRequests[taskInfo->scheduler_number]++;

            try {

                // Allocate memory on the stack that can hold a few number of transactions that are fast 
                // to allocate. The pointer to the memory will be kept on the thread. It is important that 
                // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
                // be invalid after.
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);

                // Fetching the callback.
                Action<IPAddress, UInt16, Byte[]> userCallback = udpSocketHandlers_[managedHandlerId];
                if (userCallback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                // Determining if chunk is single.
                Boolean isSingleChunk = ((taskInfo->flags & 0x01) == 0);

                Byte[] dataBytes = new Byte[*(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_NUM_BYTES)];

                // Checking if we need to process linked chunks.
                if (!isSingleChunk) {

                    fixed (Byte* fixedBuf = dataBytes) {

                        // Copying all chunks data.
                        UInt32 errorCode = bmx.sc_bmx_copy_from_chunks_and_release_trailing(
                            taskInfo->chunk_index,
                            MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA),
                            dataBytes.Length,
                            fixedBuf,
                            dataBytes.Length);

                        if (errorCode != 0)
                            throw ErrorCode.ToException(errorCode);
                    }

                } else {

                    // Copying single chunk data into managed buffer.
                    Marshal.Copy(new IntPtr(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA)), dataBytes, 0, dataBytes.Length);
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

            } catch (Exception exc) {

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Need to return all chunks here.
                bmx.sc_bmx_release_linked_chunks(&taskInfo->chunk_index);

                // Needs to be called before the stack-allocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

            return 0;
        }

        /// <summary>
        /// Handles TCP socket data.
        /// </summary>
        unsafe static UInt32 HandleTcpSocket(
            UInt16 managedHandlerId,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled)
		{
            // First marking handler as not handled.
            *isHandled = false;

            // Distribution statistics.
            SchedulerNumRequests[taskInfo->scheduler_number]++;

            // Creating network data stream object which holds the chunk etc.
            NetworkDataStream dataStream = new NetworkDataStream(
                taskInfo->chunk_index, 
                taskInfo->client_worker_id, 
                taskInfo->scheduler_number);

            TcpSocket tcpSocket = null;

            try {

                // Creating TCP socket object.
                tcpSocket = new TcpSocket(dataStream);
                Debug.Assert(null != tcpSocket);

                // Allocate memory on the stack that can hold a few number of transactions that are fast 
                // to allocate. The pointer to the memory will be kept on the thread. It is important that 
                // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
                // be invalid after.
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);

                // Fetching the callback.
                Action<TcpSocket, Byte[]> userCallback = tcpSocketHandlers_[managedHandlerId];
                if (userCallback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                Byte[] dataBytes = null;

                // Checking if its not a socket disconnect.
                if (((*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_JUST_PUSH_DISCONNECT) == 0) {

                    dataBytes = new Byte[*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_NUM_BYTES)];

                    fixed (Byte* fixedBuf = dataBytes) {

                        // Copying chunks data into plain buffer.
                        UInt32 errorCode = bmx.sc_bmx_copy_from_chunks_and_release_trailing(
                            taskInfo->chunk_index,
                            MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA),
                            dataBytes.Length,
                            fixedBuf,
                            dataBytes.Length);

                        if (errorCode != 0)
                            throw ErrorCode.ToException(errorCode);
                    }

                } else {

                    // Making socket unusable.
                    tcpSocket.Destroy(true);
                }

                if (Db.Environment.HasDatabase) {
                    TransactionManager.CreateImplicitAndSetCurrent(true);
                }

                // Destroying original chunk etc.
                dataStream.Destroy(true);

                // Calling user callback.
                userCallback(tcpSocket, dataBytes);
                
                *isHandled = true;

            } catch (Exception exc) {

                // Disconnecting the socket if there is an exception.
                if (null != tcpSocket) {
                    tcpSocket.Disconnect();
                }

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Destroying original chunk etc.
                dataStream.Destroy(true);

                // Needs to be called before the stack-allocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

			return 0;
		}

        /// <summary>
        /// Start the session that came with request.
        /// </summary>
        static Session StartSessionThatCameWithRequest(Request req) {
            IAppsSession s = null;

            // Checking if we are in session already.
            if (req.CameWithCorrectSession) {

                // Obtaining session.
                s = req.GetAppsSessionInterface();

                // Checking if correct session was obtained.
                if (null != s) {
                    // Starting session.
                    s.StartUsing();
                }
            }
            return (Session)s;
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
            // First marking handler as not handled.
            *isHandled = false;

            // Distribution statistics.
            SchedulerNumRequests[taskInfo->scheduler_number]++;

            Request req = null;

            // Creating network data stream object which holds the chunk etc.
            NetworkDataStream dataStream = new NetworkDataStream(
                taskInfo->chunk_index,
                taskInfo->client_worker_id, 
                taskInfo->scheduler_number);

            Session session = null;

            try {

                // Allocate memory on the stack that can hold a few number of transactions that are fast 
                // to allocate. The pointer to the memory will be kept on the thread. It is important that 
                // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
                // be invalid after.
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
                
                UInt32 chunkIndex = taskInfo->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                Boolean isSingleChunk = ((taskInfo->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

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
                req = new Request();

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
                    UInt16 numChunks = *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = numChunks * MixedCodeConstants.SHM_CHUNK_SIZE;

                    IntPtr plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunkIndex,
                        plainRawPtr,
                        totalBytes);

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

                // Starting the session that came with request.
                session = StartSessionThatCameWithRequest(req);

                // Setting the incoming request.
                Handle.IncomingRequest = req;

                // Processing external request.
                *isHandled = UriInjectMethods.processExternalRequest_(req);

            } catch (Exception exc) {

                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;

            } finally {

                // Destroying the chunk if there is no finalizer.
                if (null != req) {

                    if (!req.HasFinalizer()) {

                        // Destroying original chunk etc.
                        dataStream.Destroy(true);

                        // Destroying native buffers.
                        req.Destroy(true);
                    }

                } else {

                    // Destroying original chunk etc.
                    dataStream.Destroy(true);
                }

                // Restoring all outgoing request fields.
                Handle.ResetAllOutgoingFields();

                // Clearing current session.
                Session.Current = null;
                
                // Needs to be called before the stack-allocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

            return 0;
        }

        /// <summary>
        /// Registers handler with gateway.
        /// </summary>
        internal static void RegisterHttpHandlerInGateway(
            UInt16 port,
            String appName,
            String methodSpaceUri,
            Byte[] nativeParamTypes,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo) {

            Byte numParams = 0;
            if (null != nativeParamTypes)
                numParams = (Byte)nativeParamTypes.Length;

            RegisterManagedHandler(HandlerTypes.HttpHandler, managedHandlerIndex, out handlerInfo);

            String dbName = StarcounterEnvironment.DatabaseNameLower;

            String uriHandlerInfo =
                dbName + " " +
                appName + " " +
                handlerInfo + " " +
                port + " " +
                methodSpaceUri.Replace(' ', '\\') + " " +
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

            Response r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/gw/handler/uri", 
                uriHandlerInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

            if (!r.IsSuccessStatusCode) {

                String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

                if (null != errCodeStr)
                    throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                else
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
            }
        }

        /// <summary>
        /// Unregister existing URI handler.
        /// </summary>
        internal static void UnregisterHttpHandlerInGateway(UInt16 port, String methodSpaceUri) {

            String uriHandlerInfo =
                port + " " +
                methodSpaceUri.Replace(' ', '\\');

            uriHandlerInfo += "\r\n\r\n\r\n\r\n";

            Byte[] uriHandlerInfoBytes = ASCIIEncoding.ASCII.GetBytes(uriHandlerInfo);

            Response r = Http.DELETE("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/gw/handler/uri",
                uriHandlerInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

            if (!r.IsSuccessStatusCode) {

                String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

                if (null != errCodeStr)
                    throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                else
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
            }
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
            // First marking handler as not handled.
            *isHandled = false;

            // Distribution statistics.
            SchedulerNumRequests[taskInfo->scheduler_number]++;

            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            // Creating network data stream object.
            NetworkDataStream dataStream = new NetworkDataStream(
                taskInfo->chunk_index, 
                taskInfo->client_worker_id, 
                taskInfo->scheduler_number);

            // The WebSocket object on which we perform the operations.
            WebSocket ws = null;

            Session session = null;
            
            try {

                // Creating underlying socket.
                SocketStruct socketStruct = new SocketStruct();
                socketStruct.Init(dataStream);

                // Allocate memory on the stack that can hold a few number of transactions that are fast 
                // to allocate. The pointer to the memory will be kept on the thread. It is important that 
                // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
                // be invalid after.
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);

                // Determining if chunk is single.
                isSingleChunk = ((taskInfo->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                MixedCodeConstants.WebSocketDataTypes wsType = 
                    (MixedCodeConstants.WebSocketDataTypes) (*(Byte*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE));

                UInt32 groupId = (*(UInt32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_CHANNEL_ID));

                Int32 numDataBytes = *(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_NUM_BYTES);
                Int32 chunkDataOffset = MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *(Int32*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
                    UInt16 numChunks = *(UInt16*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = numChunks * MixedCodeConstants.SHM_CHUNK_SIZE;

                    // Checking that we don't exceed the buffers.
                    if (chunkDataOffset + numDataBytes > totalBytes) {
                        throw new ArgumentOutOfRangeException("if (chunkDataOffset + numDataBytes > totalBytes)");
                    }

                    plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    bmx.sc_bmx_plain_copy_and_release_chunks(taskInfo->chunk_index, plainRawPtr, totalBytes);

                    // Adjusting pointers to a new plain byte array.
                    rawChunk = plainRawPtr;
                }

                switch (wsType)
                {
                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY:
                    {
                        Byte[] dataBytes = new Byte[numDataBytes];

                        Marshal.Copy(new IntPtr(rawChunk + chunkDataOffset), dataBytes, 0, dataBytes.Length);

                        ws = new WebSocket(socketStruct, null, dataBytes, false, WebSocket.WsHandlerType.BinaryData);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT:
                    {
                        String dataString = new String(
                            (SByte*)(rawChunk + chunkDataOffset),
                            0,
                            numDataBytes,
                            Encoding.UTF8);

                        ws = new WebSocket(socketStruct, dataString, null, true, WebSocket.WsHandlerType.StringMessage);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_CLOSE:
                    {
                        ws = new WebSocket(socketStruct, null, null, false, WebSocket.WsHandlerType.Disconnect);

                        break;
                    }

                    default: {
                        throw new Exception("Unknown WebSocket frame type: " + wsType);
                    }
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
                    session = (Session)appsSession;
                    session.ActiveWebSocket = ws;
                    appsSession.StartUsing();
                }

                // Setting statically available current WebSocket.
                WebSocket.Current = ws;

                if (Db.Environment.HasDatabase) {
                    TransactionManager.CreateImplicitAndSetCurrent(true);
                }

                // Destroying original chunk etc.
                dataStream.Destroy(true);

                // Adding session reference.
                *isHandled = AllWsGroups.WsManager.RunHandler(managedHandlerId, groupId, ws);

            } catch (Exception exc) {

                // On exceptions we have to disconnect the WebSocket with a message.
                if (ws != null) {
                    ws.Disconnect(exc.ToString().Substring(0, 120), 
                        WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                }

                LogSources.Hosting.LogException(exc);

                return Error.SCERRUNSPECIFIED;

            } finally {

                // Destroying original chunk etc.
                dataStream.Destroy(true);

                // Cleaning the linear buffer in case of multiple chunks.
                if (!isSingleChunk) {

                    if (IntPtr.Zero != plainChunksData) {
                        BitsAndBytes.Free(plainChunksData);
                        plainChunksData = IntPtr.Zero;
                    }

                    rawChunk = null;
                }

                // Clearing current session.
                Session.Current = null;

                // Needs to be called before the stack-allocated array is cleared and after the session is ended.
                TransactionManager.Cleanup();                

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }

            return 0;
        }

        /// <summary>
        /// Registers the WebSocket handler.
        /// </summary>
        internal static void RegisterWsChannelHandlerInGateway(
            UInt16 port,
            String appName,
            String groupName,
            UInt32 groupId,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
        {
            // Ensuring correct multi-threading handlers creation.
            unsafe
            {
                RegisterManagedHandler(HandlerTypes.WebSocketHandler, managedHandlerIndex, out handlerInfo);

                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String uriHandlerInfo =
                    dbName + " " +
                    appName + " " +
                    handlerInfo + " " +
                    port + " " +                            
                    groupId + " " +
                    groupName + " ";

                uriHandlerInfo += "\r\n\r\n\r\n\r\n";

                Byte[] uriHandlerInfoBytes = ASCIIEncoding.ASCII.GetBytes(uriHandlerInfo);

                Response r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                    "/gw/handler/ws", uriHandlerInfoBytes, null, 0,
                    new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

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
        internal static void RegisterUdpSocketHandlerInGateway(
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
        internal static void RegisterTcpSocketHandlerInGateway(
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
            lock (tcpSocketHandlers_) {

                bmx.BMX_HANDLER_CALLBACK fp = null;

                if (udpCallback == null)
                    fp = HandleTcpSocket;
                else
                    fp = HandleUdpSocket;

                GCHandle gch = GCHandle.Alloc(fp);
                IntPtr pinnedDelegate = Marshal.GetFunctionPointerForDelegate(fp);
                
                UInt16 numHandlers = numTcpPortHandlers_;
                if (udpCallback != null)
                    numHandlers = numUdpPortHandlers_;

                if (udpCallback != null) {
                    RegisterManagedHandler(HandlerTypes.UdpHandler, numHandlers, out handlerInfo);
                } else {
                    RegisterManagedHandler(HandlerTypes.TcpHandler, numHandlers, out handlerInfo);
                }

                if (udpCallback == null) {
                    tcpSocketHandlers_[numTcpPortHandlers_] = rawCallback;
                    numTcpPortHandlers_++;
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

                Response r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                    "/gw/handler/port", portInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

                    if (null != errCodeStr)
                        throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                    else
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
                }
            }
        }

        /// <summary>
        /// Unregister port handler.
        /// </summary>
        static void UnregisterPort(UInt16 port, UInt64 handlerInfo)
		{
            // Ensuring correct multi-threading handlers creation.
            lock (tcpSocketHandlers_)
            {
                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String portInfo =
                    dbName + " " +
                    handlerInfo + " " +
                    port + " ";

                portInfo += "\r\n\r\n\r\n\r\n";

                Byte[] portInfoBytes = ASCIIEncoding.ASCII.GetBytes(portInfo);

                Response r = Http.DELETE("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                    "/gw/handler/port", portInfoBytes, null, 0, new HandlerOptions() { CallExternalOnly = true });

                if (!r.IsSuccessStatusCode) {

                    String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

                    if (null != errCodeStr)
                        throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                    else
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
                }
            }
		}
	}
}
