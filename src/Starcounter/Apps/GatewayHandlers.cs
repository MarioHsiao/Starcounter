
using System;
using System.Runtime.InteropServices;
using System.Text;
using HttpStructs;
using Starcounter.Apps.Bootstrap;
using Starcounter.Internal;

namespace Starcounter
{
    public delegate Boolean UriCallback(
        HttpRequest info
    );

    public struct PortHandlerParams
    {
        public UInt64 UserSessionId;
        public NetworkDataStream DataStream;
    }

    public delegate Boolean PortCallback(
		PortHandlerParams info
	);

    public struct SubportHandlerParams
    {
        public UInt64 UserSessionId;
        public UInt32 SubportId;
        public NetworkDataStream DataStream;
    }

    public delegate Boolean SubportCallback(
        SubportHandlerParams info
    );

	public unsafe class GatewayHandlers
	{
        // Offset in bytes for HttpRequest structure.
        const Int32 HTTP_REQUEST_OFFSET_BYTES = 192;

        // Maximum size of BMX header in the beginning of the chunk
        // after which the gateway data can be placed.
        const Int32 BMX_HEADER_MAX_SIZE_BYTES = 24;

        // Maximum number of handlers to register.
        const Int32 MAX_HANDLERS = 1024;

        private static PortCallback[] port_handlers_;
        private static SubportCallback[] subport_handlers_;
        private static UriCallback[] uri_handlers_;

		private static bmx.BMX_HANDLER_CALLBACK port_outer_handler_;
        private static bmx.BMX_HANDLER_CALLBACK subport_outer_handler_;
        private static bmx.BMX_HANDLER_CALLBACK uri_outer_handler_;

        static GatewayHandlers()
		{
            AppProcess.AssertInDatabaseOrSendStartRequest();

            port_handlers_ = new PortCallback[MAX_HANDLERS];
            subport_handlers_ = new SubportCallback[MAX_HANDLERS];
            uri_handlers_ = new UriCallback[MAX_HANDLERS];

            port_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(PortOuterHandler);
            subport_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(SubportOuterHandler);
            uri_outer_handler_ = new bmx.BMX_HANDLER_CALLBACK(UriOuterHandler);
		}

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

			return 0;
		}

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

            return 0;
        }

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
            UriCallback user_callback = uri_handlers_[task_info->handler_id];
            if (user_callback == null)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED); // SCERRHANDLERNOTFOUND

            // Determining if chunk is single.
            Boolean is_single_chunk = ((task_info->flags & 0x01) == 0);

            // Creating network data stream object.
            NetworkDataStream data_stream = new NetworkDataStream();

            // Obtaining HttpRequest structure.
            HttpRequest http_request = new HttpRequest(
                raw_chunk,
                is_single_chunk,
                task_info->chunk_index,
                raw_chunk + BMX_HEADER_MAX_SIZE_BYTES + HTTP_REQUEST_OFFSET_BYTES,
                raw_chunk + BMX_HEADER_MAX_SIZE_BYTES,
                data_stream);
            
            // Calling user callback.
            *is_handled = user_callback(http_request);

            return 0;
        }

        // Registers port handler.
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
        public static void RegisterUriHandler(
            UInt16 port,
            String uri_string,
            HTTP_METHODS http_method,
            UriCallback uriCallback,
            out UInt16 handlerId)
        {
            UInt16 handler_id;

            // Ensuring correct multi-threading handlers creation.
            lock (port_handlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_register_uri_handler(port, uri_string, (Byte)http_method, uri_outer_handler_, &handler_id);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode);

                uri_handlers_[handler_id] = uriCallback;
                handlerId = handler_id;
            }
        }
	}
}
