﻿// ***********************************************************************
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
    /// Class GatewayHandlers
    /// </summary>
	public unsafe class GatewayHandlers
	{
        /// <summary>
        /// Maximum number of user handlers to register.
        /// </summary>
        const Int32 MAX_HANDLERS = 1024;

        /// <summary>
        /// The port_handlers_
        /// </summary>
        private static RawSocketCallback[] raw_port_handlers_;

        /// <summary>
        /// Number of registered raw port handlers.
        /// </summary>
        static UInt16 num_raw_port_handlers_ = 0;

        /// <summary>
        /// Initializes static members of the <see cref="GatewayHandlers" /> class.
        /// </summary>
        static GatewayHandlers()
		{
            raw_port_handlers_ = new RawSocketCallback[MAX_HANDLERS];
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
            UInt16 managed_handler_id,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
		{
            Boolean is_single_chunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            try {

                *is_handled = false;

                UInt32 chunkIndex = task_info->chunk_index;

                // Fetching the callback.
                RawSocketCallback user_callback = raw_port_handlers_[managed_handler_id];
                if (user_callback == null)
                    throw ErrorCode.ToException(Error.SCERRHANDLERNOTFOUND);

                // Determining if chunk is single.
                is_single_chunk = ((task_info->flags & 0x01) == 0);

                NetworkDataStream dataStream = new NetworkDataStream(rawChunk, task_info->chunk_index, task_info->client_worker_id);

                // Checking if we need to process linked chunks.
                if (!is_single_chunk) {

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
                user_callback(rawSocket, dataBytes);

                // Destroying original chunk etc.
                rawSocket.DestroyDataStream();

                *is_handled = true;

                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();

            } finally {

                // Cleaning the linear buffer in case of multiple chunks.
                if (!is_single_chunk) {

                    BitsAndBytes.Free(plainChunksData);
                    plainChunksData = IntPtr.Zero;
                    rawChunk = null;
                }

                // Clearing current session.
                Session.End();
            }

			return 0;
		}

        static String AggrRespString =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "Content-Length: 10\r\n\r\n1234567890";

        static Byte[] AggrRespBytes = UTF8Encoding.ASCII.GetBytes(AggrRespString);

        /// <summary>
        /// This is the main entry point of incoming HTTP requests.
        /// It is called from the Gateway via the shared memory IPC (interprocess communication).
        /// </summary>
        internal unsafe static UInt32 HandleHttpRequest(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            Boolean is_single_chunk = false;

            try
            {
                *is_handled = false;

                UInt32 chunk_index = task_info->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                is_single_chunk = ((task_info->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                // Socket data begin.
                Byte* socket_data_begin = raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

                // Checking if we are accumulating on host.
                if (((*(UInt32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION) != 0)
                {

                }

                // Getting aggregation flag.
                Boolean isAggregated = (((*(UInt32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.SOCKET_DATA_FLAGS_AGGREGATED) != 0);

                // Checking if flag to upgrade to WebSockets is set.
                Boolean wsUpgradeRequest = (((*(UInt32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_FLAGS)) & (UInt32)MixedCodeConstants.SOCKET_DATA_FLAGS.HTTP_WS_FLAGS_UPGRADE_REQUEST) != 0);

                Request http_request = null;

                // Checking if we need to process linked chunks.
                if (!is_single_chunk)
                {
                    // Creating network data stream object.
                    NetworkDataStream data_stream = new NetworkDataStream(raw_chunk, task_info->chunk_index, task_info->client_worker_id);

                    UInt16 num_chunks = *(UInt16*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    Int32 totalBytes = num_chunks * MixedCodeConstants.SHM_CHUNK_SIZE;
                    IntPtr plainChunksData = BitsAndBytes.Alloc(totalBytes);

                    Byte* plainRawPtr = (Byte*) plainChunksData.ToPointer();

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunk_index,
                        raw_chunk,
                        plainRawPtr,
                        totalBytes);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Obtaining Request structure.
                    http_request = new Request(
                        raw_chunk,
                        is_single_chunk,
                        chunk_index,
                        managed_handler_id,
                        plainRawPtr + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        plainRawPtr + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA,
                        plainChunksData,
                        data_stream,
                        wsUpgradeRequest,
                        isAggregated);
                }
                else
                {
                    // Creating network data stream object.
                    NetworkDataStream data_stream = new NetworkDataStream(raw_chunk, task_info->chunk_index, task_info->client_worker_id);

                    /*if (isAggregated) {
                        data_stream.SendResponse(AggrRespBytes, 0, AggrRespBytes.Length, Response.ConnectionFlags.NoSpecialFlags);

                        return 0;
                    }*/

                    // Obtaining Request structure.
                    http_request = new Request(
                        raw_chunk,
                        is_single_chunk,
                        task_info->chunk_index,
                        managed_handler_id,
                        socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        socket_data_begin,
                        IntPtr.Zero,
                        data_stream,
                        wsUpgradeRequest,
                        isAggregated);
                }

                // Calling user callback.
                *is_handled = UriInjectMethods.OnHttpMessageRoot_(http_request);
            
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
            UInt16 managed_handler_id,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            Boolean isSingleChunk = false;
            IntPtr plainChunksData = IntPtr.Zero;

            try
            {
                *is_handled = false;

                UInt32 chunkIndex = task_info->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                isSingleChunk = ((task_info->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                MixedCodeConstants.WebSocketDataTypes wsType = 
                    (MixedCodeConstants.WebSocketDataTypes) (*(Byte*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE));

                WebSocket ws = null;

                // Creating network data stream object.
                NetworkDataStream data_stream = new NetworkDataStream(rawChunk, task_info->chunk_index, task_info->client_worker_id);

                SchedulerResources.SocketContainer sc = SchedulerResources.ObtainSocketContainerForWebSocket(data_stream);

                // Checking if WebSocket exists and legal.
                if (sc == null) {
                    data_stream.Destroy(true);
                    return 0;
                }

                WebSocketInternal wsInternal = sc.Ws;
                Debug.Assert(null != wsInternal);
                Debug.Assert(null != wsInternal.SocketContainer);

                // Checking if we need to process linked chunks.
                if (!isSingleChunk)
                {
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

                ScSessionStruct session_ = 
                    *(ScSessionStruct*)(rawChunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);

                // Obtaining corresponding Apps session.
                IAppsSession apps_session = GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref session_);

                // Searching the existing session.
                ws.Session = apps_session;

                // Starting session.
                if (apps_session != null)
                {
                    Session session = (Session)apps_session;
                    session.ActiveWebsocket = ws;
                    Session.Start(session);
                }

                // Setting statically available current WebSocket.
                WebSocket.Current = ws;

                Debug.Assert(null != wsInternal.SocketContainer);

                // Adding session reference.
                *is_handled = AllWsChannels.WsManager.RunHandler(managed_handler_id, ws);

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
                IntPtr pinned_delegate = Marshal.GetFunctionPointerForDelegate(fp);

                UInt32 errorCode = bmx.sc_bmx_register_ws_handler(
                    port,
                    appName,
                    channelName,
                    channelId,
                    pinned_delegate,
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
        /// Registers the raw port handler.
        /// </summary>
        public static void RegisterRawPortHandler(
			UInt16 port,
            String appName,
			RawSocketCallback portCallback,
            out UInt64 handlerInfo)
		{
            // Ensuring correct multi-threading handlers creation.
            lock (raw_port_handlers_)
            {
                bmx.BMX_HANDLER_CALLBACK fp = HandleRawSocket;
                GCHandle gch = GCHandle.Alloc(fp);
                IntPtr pinned_delegate = Marshal.GetFunctionPointerForDelegate(fp);

                UInt32 errorCode = bmx.sc_bmx_register_port_handler(port, appName, pinned_delegate, num_raw_port_handlers_, out handlerInfo);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port);

                raw_port_handlers_[num_raw_port_handlers_] = portCallback;
                num_raw_port_handlers_++;

                String dbName = StarcounterEnvironment.DatabaseNameLower;

                String portInfo =
                    dbName + " " +
                    appName + " " +
                    handlerInfo + " " +
                    port + " ";

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
            lock (raw_port_handlers_)
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
