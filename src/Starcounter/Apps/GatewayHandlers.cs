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

namespace Starcounter
{
    /// <summary>
    /// Struct PortHandlerParams
    /// </summary>
    public struct PortHandlerParams
    {
        /// <summary>
        /// The user session id
        /// </summary>
        public UInt64 UserSessionId;

        /// <summary>
        /// The data stream
        /// </summary>
        public NetworkDataStream DataStream;
    }

    /// <summary>
    /// Delegate PortCallback
    /// </summary>
    /// <param name="info">The info.</param>
    /// <returns>Boolean.</returns>
    public delegate Boolean PortCallback(
		PortHandlerParams info
	);

    /// <summary>
    /// Struct SubportHandlerParams
    /// </summary>
    public struct SubportHandlerParams
    {
        /// <summary>
        /// The user session id
        /// </summary>
        public UInt64 UserSessionId;

        /// <summary>
        /// The subport id
        /// </summary>
        public UInt32 SubportId;

        /// <summary>
        /// The data stream
        /// </summary>
        public NetworkDataStream DataStream;
    }

    /// <summary>
    /// Delegate SubportCallback
    /// </summary>
    /// <param name="info">The info.</param>
    /// <returns>Boolean.</returns>
    public delegate Boolean SubportCallback(
        SubportHandlerParams info
    );

    /// <summary>
    /// Class GatewayHandlers
    /// </summary>
	internal unsafe class GatewayHandlers
	{
        /// <summary>
        /// Maximum number of user handlers to register.
        /// </summary>
        const Int32 MAX_HANDLERS = 1024;

        /// <summary>
        /// The port_handlers_
        /// </summary>
        private static PortCallback[] port_handlers_;
        /// <summary>
        /// The subport_handlers_
        /// </summary>
        private static SubportCallback[] subport_handlers_;
        
        /// <summary>
        /// The outer port handler.
        /// </summary>
		private static bmx.BMX_HANDLER_CALLBACK port_outer_handler_;
        
        /// <summary>
        /// The outer sub-port handler.
        /// </summary>
        private static bmx.BMX_HANDLER_CALLBACK subport_outer_handler_;

        /// <summary>
        /// Initializes static members of the <see cref="GatewayHandlers" /> class.
        /// </summary>
        static GatewayHandlers()
		{
            port_handlers_ = new PortCallback[MAX_HANDLERS];
            subport_handlers_ = new SubportCallback[MAX_HANDLERS];

            port_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(PortOuterHandler);
            subport_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(SubportOuterHandler);
		}

        /// <summary>
        /// Ports the outer handler.
        /// </summary>
        /// <param name="session_id">The session_id.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="task_info">The task_info.</param>
        /// <param name="is_handled">The is_handled.</param>
        /// <returns>UInt32.</returns>
        private unsafe static UInt32 PortOuterHandler(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
		{
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            PortCallback user_callback = port_handlers_[managed_handler_id];
			if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Releasing linked chunks if not single.
            if (!is_single_chunk)
                throw new NotImplementedException();

            // Creating parameters.
            PortHandlerParams handler_params = new PortHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SESSION_LINEAR_INDEX),
                DataStream = new NetworkDataStream(raw_chunk, task_info->chunk_index, task_info->client_worker_id)
            };

            // Calling user callback.
            *is_handled = user_callback(handler_params);
            
            // Reset managed task state before exiting managed task entry point.
            TaskHelper.Reset();

			return 0;
		}

        /// <summary>
        /// Subports the outer handler.
        /// </summary>
        /// <param name="session_id">The session_id.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="task_info">The task_info.</param>
        /// <param name="is_handled">The is_handled.</param>
        /// <returns>UInt32.</returns>
        private unsafe static UInt32 SubportOuterHandler(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            SubportCallback user_callback = subport_handlers_[managed_handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Releasing linked chunks if not single.
            if (!is_single_chunk)
                throw new NotImplementedException();

            // Creating parameters.
            SubportHandlerParams handler_params = new SubportHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SESSION_LINEAR_INDEX),
                SubportId = 0,
                DataStream = new NetworkDataStream(raw_chunk, task_info->chunk_index, task_info->client_worker_id)
            };

            // Calling user callback.
            *is_handled = user_callback(handler_params);
            
            // Reset managed task state before exiting managed task entry point.
            TaskHelper.Reset();

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
        internal unsafe static UInt32 HandleIncomingHttpRequest(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            try
            {
                *is_handled = false;

                UInt32 chunk_index = task_info->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                Boolean is_single_chunk = ((task_info->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

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
                    IntPtr plain_chunks_data = BitsAndBytes.Alloc(num_chunks * MixedCodeConstants.SHM_CHUNK_SIZE);

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunk_index,
                        raw_chunk,
                        (Byte*) plain_chunks_data);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Obtaining Request structure.
                    http_request = new Request(
                        raw_chunk,
                        is_single_chunk,
                        chunk_index,
                        managed_handler_id,
                        (Byte*) plain_chunks_data + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        (Byte*) plain_chunks_data + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA,
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
                        data_stream,
                        wsUpgradeRequest,
                        isAggregated);
                }

                // Calling user callback.
                *is_handled = UriInjectMethods.OnHttpMessageRoot_(http_request);
            
                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }
            catch (Exception exc)
            {
                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;
            }

            return 0;
        }

        /// <summary>
        /// This is the main entry point of incoming WebSocket requests.
        /// It is called from the Gateway via the shared memory IPC (interprocess communication).
        /// </summary>
        internal unsafe static UInt32 HandleWebSocket(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            try
            {
                *is_handled = false;

                UInt32 chunk_index = task_info->chunk_index;
                //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

                // Determining if chunk is single.
                Boolean is_single_chunk = ((task_info->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

                Byte* socket_data_begin = raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;

                MixedCodeConstants.WebSocketDataTypes wsType = (MixedCodeConstants.WebSocketDataTypes) (*(Byte*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_WS_OPCODE));

                WebSocket ws = null;

                // Creating network data stream object.
                NetworkDataStream data_stream = new NetworkDataStream(raw_chunk, task_info->chunk_index, task_info->client_worker_id);

                // Checking if WebSocket is legitimate.
                WebSocketInternal wsInternal = WebSocket.ObtainWebSocketInternal(data_stream);
                if (wsInternal == null)
                {
                    data_stream.Destroy(true);
                    return 0;
                }

                // Checking if we need to process linked chunks.
                if (!is_single_chunk)
                {
                    UInt16 num_chunks = *(UInt16*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_NUM_IPC_CHUNKS);

                    // Allocating space to copy linked chunks (freed on Request destruction).
                    IntPtr plain_chunks_data = BitsAndBytes.Alloc(num_chunks * MixedCodeConstants.SHM_CHUNK_SIZE);

                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunk_index,
                        raw_chunk,
                        (Byte*)plain_chunks_data);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Adjusting pointers to a new plain byte array.
                    raw_chunk = (Byte*) plain_chunks_data;
                    socket_data_begin = raw_chunk + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA;
                }

                switch (wsType)
                {
                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_BINARY:
                    {
                        Byte[] dataBytes = new Byte[*(Int32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_LEN)];

                        Marshal.Copy((IntPtr)(socket_data_begin + *(UInt16*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD)), dataBytes, 0, dataBytes.Length);

                        ws = new WebSocket(wsInternal, data_stream, null, dataBytes, WebSocket.WsHandlerType.BinaryData);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_TEXT:
                    {
                        String dataString = new String(
                            (SByte*)(socket_data_begin + *(UInt16*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_OFFSET_IN_SD)),
                            0,
                            *(Int32*)(raw_chunk + MixedCodeConstants.CHUNK_OFFSET_WS_PAYLOAD_LEN),
                            Encoding.UTF8);

                        ws = new WebSocket(wsInternal, data_stream, dataString, null, WebSocket.WsHandlerType.StringMessage);

                        break;
                    }

                    case MixedCodeConstants.WebSocketDataTypes.WS_OPCODE_CLOSE:
                    {
                        ws = new WebSocket(wsInternal, data_stream, null, null, WebSocket.WsHandlerType.Disconnect);

                        break;
                    }

                    default:
                        throw new Exception("Unknown WebSocket frame type: " + wsType);
                }

                // Cleaning the linear buffer in case of multiple chunks.
                if (!is_single_chunk)
                {
                    BitsAndBytes.Free((IntPtr)raw_chunk);
                }

                ScSessionStruct* session_ = (ScSessionStruct*)(socket_data_begin + MixedCodeConstants.SOCKET_DATA_OFFSET_SESSION);

                // Obtaining corresponding Apps session.
                IAppsSession apps_session =
                    GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref *session_);

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

                // Adding session reference.
                *is_handled = AllWsChannels.WsManager.RunHandler(managed_handler_id, ws);

                // Destroying original chunk etc.
                ws.ManualDestroy();
            
                // Reset managed task state before exiting managed task entry point.
                TaskHelper.Reset();
            }
            catch (Exception exc)
            {
                LogSources.Hosting.LogException(exc);
                return Error.SCERRUNSPECIFIED;
            }

            return 0;
        }

        /// <summary>
        /// Registers the port handler.
        /// </summary>
        public static void RegisterPortHandler(
			UInt16 port,
            String appName,
			PortCallback portCallback,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
		{
            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_port_handler(port, appName, port_outer_handler_, managedHandlerIndex, out handlerInfo);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port);

                port_handlers_[managedHandlerIndex] = portCallback;
            }
		}

        public static void UnregisterPort(UInt16 port)
		{
            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_unregister_port(port);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port);
            }
		}

        /// <summary>
        /// Registers the subport handler.
        /// </summary>
        public static void RegisterSubportHandler(
            UInt16 port,
            String appName,
            UInt32 subport,
            SubportCallback subportCallback,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
        {
            // Ensuring correct multi-threading handlers creation.
            lock (subport_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_subport_handler(port, appName, subport, subport_outer_handler_, managedHandlerIndex, out handlerInfo);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port + ", Sub-port number: " + subport);

                subport_handlers_[managedHandlerIndex] = subportCallback;
            }
        }

        public static void UnregisterSubport(
            UInt16 port,
            UInt32 subport)
        {
            // Ensuring correct multi-threading handlers creation.
            lock (subport_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_unregister_subport(port, subport);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Port number: " + port + ", Sub-port number: " + subport);
            }
        }
	}
}
