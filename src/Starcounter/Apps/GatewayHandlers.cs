// ***********************************************************************
// <copyright file="GatewayHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Text;
using HttpStructs;
using Starcounter.Internal;
using Starcounter.Advanced;

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
	public unsafe class GatewayHandlers
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
        /// The uri_handlers_
        /// </summary>
        private static UserHandlerCodegen.UriCallbackDelegate[] uri_handlers_;

        /// <summary>
        /// The port_outer_handler_
        /// </summary>
		private static bmx.BMX_HANDLER_CALLBACK port_outer_handler_;
        /// <summary>
        /// The subport_outer_handler_
        /// </summary>
        private static bmx.BMX_HANDLER_CALLBACK subport_outer_handler_;
        /// <summary>
        /// The uri_outer_handler_
        /// </summary>
        private static bmx.BMX_HANDLER_CALLBACK uri_outer_handler_;

        /// <summary>
        /// Initializes static members of the <see cref="GatewayHandlers" /> class.
        /// </summary>
        static GatewayHandlers()
		{
            port_handlers_ = new PortCallback[MAX_HANDLERS];
            subport_handlers_ = new SubportCallback[MAX_HANDLERS];
            uri_handlers_ = new UserHandlerCodegen.UriCallbackDelegate[MAX_HANDLERS];

            port_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(PortOuterHandler);
            subport_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(SubportOuterHandler);
            uri_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(UriOuterHandler);
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
			UInt64 session_id,
			Byte* raw_chunk, 
			bmx.BMX_TASK_INFO* task_info,
			Boolean* is_handled)
		{
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            PortCallback user_callback = port_handlers_[task_info->handler_id];
			if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Creating parameters.
            PortHandlerParams handler_params = new PortHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + NetworkDataStream.SESSION_INDEX_OFFSET),
                DataStream = new NetworkDataStream(raw_chunk, is_single_chunk, task_info->chunk_index)
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
            UInt64 session_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            SubportCallback user_callback = subport_handlers_[task_info->handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Creating parameters.
            SubportHandlerParams handler_params = new SubportHandlerParams
            {
                UserSessionId = *(UInt32*)(raw_chunk + NetworkDataStream.SESSION_INDEX_OFFSET),
                SubportId = 0,
                DataStream = new NetworkDataStream(raw_chunk, is_single_chunk, task_info->chunk_index)
            };

            // Calling user callback.
            *is_handled = user_callback(handler_params);
            
            // Reset managed task state before exiting managed task entry point.
            TaskHelper.Reset();

            return 0;
        }

        /// <summary>
        /// URIs the outer handler.
        /// </summary>
        /// <param name="session_id">The session_id.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="task_info">The task_info.</param>
        /// <param name="is_handled">The is_handled.</param>
        /// <returns>UInt32.</returns>
        private unsafe static UInt32 UriOuterHandler(
            UInt64 session_id,
            Byte* raw_chunk,
            bmx.BMX_TASK_INFO* task_info,
            Boolean* is_handled)
        {
            *is_handled = false;

            UInt32 chunk_index = task_info->chunk_index;
            //Console.WriteLine("Handler called, session: " + session_id + ", chunk: " + chunk_index);

            // Fetching the callback.
            UserHandlerCodegen.UriCallbackDelegate user_callback = uri_handlers_[task_info->handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & MixedCodeConstants.LINKED_CHUNKS_FLAG) == 0);

            // Creating network data stream object.
            NetworkDataStream data_stream = new NetworkDataStream();

            // Checking if we need to process linked chunks.
            if (!is_single_chunk)
            {
                UInt32 num_chunks = *(UInt32*)(raw_chunk + MixedCodeConstants.BMX_HEADER_MAX_SIZE_BYTES + MixedCodeConstants.SOCKET_DATA_OFFSET_NUM_CHUNKS);

                Byte[] plain_chunks_data = new Byte[num_chunks * MixedCodeConstants.SHM_CHUNK_SIZE];

                fixed (Byte* p_plain_chunks_data = plain_chunks_data)
                {
                    // Copying all chunks data.
                    UInt32 errorCode = bmx.sc_bmx_plain_copy_and_release_chunks(
                        chunk_index,
                        raw_chunk,
                        p_plain_chunks_data);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode);

                    // Obtaining HttpRequest structure.
                    HttpRequest http_request = new HttpRequest(
                        raw_chunk,
                        is_single_chunk,
                        task_info->chunk_index,
                        task_info->handler_id,
                        p_plain_chunks_data + MixedCodeConstants.BMX_HEADER_MAX_SIZE_BYTES + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                        p_plain_chunks_data + MixedCodeConstants.BMX_HEADER_MAX_SIZE_BYTES,
                        data_stream);

                    // Calling user callback.
                    *is_handled = user_callback(http_request);
                }
            }
            else
            {
                // Obtaining HttpRequest structure.
                HttpRequest http_request = new HttpRequest(
                    raw_chunk,
                    is_single_chunk,
                    task_info->chunk_index,
                    task_info->handler_id,
                    raw_chunk + MixedCodeConstants.BMX_HEADER_MAX_SIZE_BYTES + MixedCodeConstants.SOCKET_DATA_OFFSET_HTTP_REQUEST,
                    raw_chunk + MixedCodeConstants.BMX_HEADER_MAX_SIZE_BYTES,
                    data_stream);

                // Calling user callback.
                *is_handled = user_callback(http_request);
            }
            
            // Reset managed task state before exiting managed task entry point.
            TaskHelper.Reset();

            return 0;
        }

        // Registers port handler.
        /// <summary>
        /// Registers the port handler.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="portCallback">The port callback.</param>
        /// <param name="handlerId">The handler id.</param>
        public static void RegisterPortHandler(
			UInt16 port, 
			PortCallback portCallback,
            out UInt16 handlerId)
		{
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_port_handler(port, port_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                port_handlers_[handler_id] = portCallback;
                handlerId = handler_id;
            }
		}

        // Registers subport handler.
        /// <summary>
        /// Registers the subport handler.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="subport">The subport.</param>
        /// <param name="subportCallback">The subport callback.</param>
        /// <param name="handlerId">The handler id.</param>
        public static void RegisterSubportHandler(
            UInt16 port,
            UInt32 subport,
            SubportCallback subportCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_subport_handler(port, subport, subport_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                subport_handlers_[handler_id] = subportCallback;
                handlerId = handler_id;
            }
        }

        // Registers URI handler.
        /// <summary>
        /// Registers the URI handler.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="uri_string">The uri_string.</param>
        /// <param name="uriCallback">The URI callback.</param>
        /// <param name="handlerId">The handler id.</param>
        public static void RegisterUriHandler(
            UInt16 port,
            String uri_string,
            //HTTP_METHODS http_method,
            UserHandlerCodegen.UriCallbackDelegate uriCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;

            // Checking for root URI special case.
            //if (String.IsNullOrEmpty(uri_string))
            //    uri_string = "/";

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_uri_handler(port, uri_string, (Byte)/*http_method*/HTTP_METHODS.OTHER_METHOD, uri_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                uri_handlers_[handler_id] = uriCallback;
                handlerId = handler_id;
            }
        }

        /// <summary>
        /// Registers the URI handler.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="uri_string">The uri_string.</param>
        /// <param name="uriCallback">The URI callback.</param>
        /// <param name="handlerId">The handler id.</param>
        public static void RegisterUriHandlerNew(
            UInt16 port,
            String uriInfo,
            Byte[] paramTypes,
            UserHandlerCodegen.UriCallbackDelegate uriCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;
            Byte numParams = 0;
            if (null != paramTypes)
                numParams = (Byte)paramTypes.Length;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                unsafe
                {
                    fixed (Byte* pp = paramTypes)
                    {
                        UInt32 errorCode = bmx.sc_bmx_register_uri_handler_new(
                            port,
                            uriInfo,
                            (Byte)/*http_method*/HTTP_METHODS.OTHER_METHOD,
                            pp,
                            numParams,
                            uri_outer_handler_,
                            &handler_id);

                        if (errorCode != 0)
                            throw ErrorCode.ToException(errorCode);
                    }
                }

                uri_handlers_[handler_id] = uriCallback;
                handlerId = handler_id;
            }
        }
	}
}
