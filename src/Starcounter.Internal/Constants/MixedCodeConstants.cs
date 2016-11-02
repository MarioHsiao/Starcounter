#if __cplusplus

#pragma once

// C++ code--removes public keyword for C++
#define public

#undef DELETE
typedef char* const String;
typedef unsigned int uint;
typedef uint64_t UInt64;

namespace starcounter
{
namespace MixedCodeConstants
{

#else

// C# code
using System;
using System.Runtime.InteropServices;
namespace Starcounter.Internal
{
    public class MixedCodeConstants
    {
        //... C# Specific class code

#endif

        // These items are members of the shared C# class and global in C++.

        /// <summary>
        /// Maximum size of BMX header in the beginning of the chunk
        /// after which the gateway data can be placed.
        /// </summary>
        public const int BMX_HEADER_MAX_SIZE_BYTES = 32;

        /// <summary>
        /// Offset of socket data in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_SOCKET_DATA = BMX_HEADER_MAX_SIZE_BYTES;

        /// <summary>
        /// Data offset/size constants. 
        /// </summary>
        public const int BMX_HANDLER_SIZE = 2;

        /// <summary>
        /// Invalid URI matcher handler index.
        /// </summary>
        public const int InvalidUriMatcherHandlerId = -1;

        /// <summary>
        /// Session string length in characters.
        /// </summary>
        public const int SESSION_STRING_LEN_CHARS = 24;

        /// <summary>
        /// Size of the session structure in bytes.
        /// </summary>
        public const int SESSION_STRUCT_SIZE = 16;

        /// <summary>
        /// Offsets in gateway socket data.
        /// </summary>
        public enum SOCKET_DATA_FLAGS
        {
            SOCKET_DATA_FLAGS_TO_DATABASE_DIRECTION = 1,
            SOCKET_DATA_FLAGS_SOCKET_REPRESENTER = 2,
            SOCKET_DATA_FLAGS_ACCUMULATING_STATE = 2 << 1,
            SOCKET_DATA_FLAGS_DISCONNECT_AFTER_SEND = 2 << 2,
            SOCKET_DATA_INTERNAL_REQUEST = 2 << 3,
            SOCKET_DATA_FLAGS_JUST_SEND = 2 << 4,
            SOCKET_DATA_FLAGS_JUST_DISCONNECT = 2 << 5,
            SOCKET_DATA_FLAGS_TRIGGER_DISCONNECT = 2 << 6,
            // Free slot here 2 << 7.
            HTTP_WS_FLAGS_PROXIED_SERVER_SOCKET = 2 << 8,
            HTTP_WS_FLAGS_UNKNOWN_PROXIED_PROTO = 2 << 9,
            HTTP_WS_FLAGS_GRACEFULLY_CLOSE = 2 << 10,
            SOCKET_DATA_FLAGS_AGGREGATED = 2 << 11,
            SOCKET_DATA_FLAGS_ON_HOST_ACCUMULATION = 2 << 12,
            HTTP_WS_FLAGS_UPGRADE_APPROVED = 2 << 13,
            HTTP_WS_FLAGS_UPGRADE_REQUEST = 2 << 14,
            HTTP_WS_JUST_PUSH_DISCONNECT = 2 << 16,
            SOCKET_DATA_GATEWAY_NO_IPC_TEST = 2 << 17,
            SOCKET_DATA_GATEWAY_AND_IPC_TEST = 2 << 18,
            SOCKET_DATA_GATEWAY_NO_IPC_NO_CHUNKS_TEST = 2 << 19,
            SOCKET_DATA_HOST_LOOPING_CHUNKS = 2 << 20,
            SOCKET_DATA_STREAMING_RESPONSE_BODY = 2 << 21
        };

        /// <summary>
        /// Enum HTTP_METHODS
        /// </summary>
        public enum HTTP_METHODS
        {
            GET,
            POST,
            PUT,
            PATCH,
            DELETE,
            HEAD,
            OPTIONS,
            TRACE,
            OTHER
        };

        /// <summary>
        /// Types of messages used in aggregation.
        /// </summary>
        public enum AggregationMessageTypes
        {
            AGGR_CREATE_SOCKET,
            AGGR_DESTROY_SOCKET,
            AGGR_DATA
        };

        /// <summary>
        /// Different specific flags for aggregation.
        /// </summary>
        public enum AggregationMessageFlags {
            AGGR_MSG_NO_FLAGS,
            AGGR_MSG_GATEWAY_NO_IPC,
            AGGR_MSG_GATEWAY_AND_IPC,
            AGGR_MSG_GATEWAY_NO_IPC_NO_CHUNKS
        };

        /// <summary>
        /// Invalid chunk index.
        /// </summary>
        public const uint INVALID_CHUNK_INDEX = 0xFFFFFFFF;
        
        /// <summary>
        /// Offsets in socket data and chunk.
        /// </summary>

        public const int SOCKET_DATA_OFFSET_SESSION = 56;
        public const int CHUNK_OFFSET_SESSION = 88;
        public const int CHUNK_OFFSET_SESSION_SCHEDULER_ID = 100;
        public const int CHUNK_OFFSET_SESSION_LINEAR_INDEX = 96;
        public const int CHUNK_OFFSET_SESSION_RANDOM_SALT = 88;
        public const int SOCKET_DATA_OFFSET_PARAMS_INFO = 152;
        public const int SOCKET_DATA_OFFSET_BLOB = 216;
        public const int CHUNK_OFFSET_NUM_IPC_CHUNKS = 32;
        public const int CHUNK_OFFSET_SOCKET_FLAGS = 124;
        public const int SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE = 113;
        public const int SOCKET_DATA_OFFSET_CLIENT_IP = 40;
        public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = 120;
        public const int SOCKET_DATA_NUM_CLONE_BYTES = 152;
        public const int CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA = 132;
        public const int CHUNK_OFFSET_USER_DATA_NUM_BYTES = 136;
        public const int SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID = 32;
        public const int SOCKET_DATA_OFFSET_SOCKET_INDEX_NUMBER = 88;
        public const int SOCKET_DATA_OFFSET_WS_OPCODE = 115;
        public const int SOCKET_DATA_OFFSET_BOUND_WORKER_ID = 69;
        public const int SOCKET_DATA_OFFSET_WS_CHANNEL_ID = 152;
        public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_SOCKADDR = 152;
        public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_IP = 156;
        public const int SOCKET_DATA_OFFSET_UDP_DESTINATION_PORT = 154;
        public const int SOCKET_DATA_OFFSET_UDP_SOURCE_PORT = 168;
        public const int CHUNK_OFFSET_UPGRADE_PART_BYTES_TO_DB = 104;
        public const int CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH_FROM_DB = 140;

        // Invalid WebSocket channel ID.
        public const int INVALID_WS_CHANNEL_ID = 0;

        // Maximum unique socket id.
        public const UInt64 MAX_UNIQUE_SOCKET_ID = 0x3FFFFFFFF;

        // Maximum socket index.
        public const int MAX_SOCKET_INDEX = 0xFFFFF;

        /// <summary>
        /// Maxiumum total number of handlers.
        /// </summary>
        public const int MAX_TOTAL_NUMBER_OF_HANDLERS = 2048;

        /// <summary>
        /// Maxiumum total number of bytes for URI codegen.
        /// </summary>
        public const int MAX_URI_MATCHING_CODE_BYTES = 1024 * 1024 * 4;

        /// <summary>
        /// Maximum number of URI callback parameters.
        /// </summary>
        public const int MAX_URI_CALLBACK_PARAMS = 16;

        /// <summary>
        /// Parameters info max size bytes.
        /// </summary>
        public const int PARAMS_INFO_MAX_SIZE_BYTES = 64;

        /// <summary>
        /// Shared memory chunk size.
        /// </summary>
        public const int SHM_CHUNK_SIZE = 1024;

        /// <summary>
        /// Shared memory chunks default number.
        /// </summary>
        public const int SHM_CHUNKS_DEFAULT_NUMBER = 1 << 16;

        /// <summary>
        /// Linked chunk flag.
        /// </summary>
        public const int LINKED_CHUNKS_FLAG = 1;

        /// <summary>
        /// Number of clone bytes in chunk.
        /// </summary>
        public const int CHUNK_NUM_CLONE_BYTES = CHUNK_OFFSET_SOCKET_DATA + SOCKET_DATA_NUM_CLONE_BYTES;

        /// <summary>
        /// Chunk link size.
        /// </summary>
        public const int CHUNK_LINK_SIZE = 8;

        /// <summary>
        /// Chunk data max size.
        /// </summary>
        public const int CHUNK_MAX_DATA_BYTES = SHM_CHUNK_SIZE - CHUNK_LINK_SIZE;

        /// <summary>
        /// Socket data max size.
        /// </summary>
        public const int SOCKET_DATA_MAX_SIZE = CHUNK_MAX_DATA_BYTES - CHUNK_OFFSET_SOCKET_DATA;

        /// <summary>
        /// Maximum extra linked IPC chunks.
        /// </summary>
        public const int MAX_EXTRA_LINKED_IPC_CHUNKS = 64;

        /// <summary>
        /// Maximum linked chunks bytes.
        /// </summary>
        public const int MAX_BYTES_EXTRA_LINKED_IPC_CHUNKS = MAX_EXTRA_LINKED_IPC_CHUNKS * CHUNK_MAX_DATA_BYTES;

        /// <summary>
        /// Size of socket data blob.
        /// </summary>
        public const int SOCKET_DATA_BLOB_SIZE_BYTES = SOCKET_DATA_MAX_SIZE - SOCKET_DATA_OFFSET_BLOB;

        // Maximum URI string length.
        public const int MAX_URI_STRING_LEN = 1024;

        // Session native parameter type number in user delegate.
        public const int REST_ARG_SESSION = 12;

        // String native parameter type number in user delegate.
        public const int REST_ARG_STRING = 0;

        // Bad server log handler.
        public const int INVALID_SERVER_LOG_HANDLE = 0;

        /// <summary>
        /// End of internal requests.
        /// </summary>
        public const String EndOfRequest = "--- End Of Request ---";

        /// <summary>
        /// Example of string constant.
        /// </summary>
        public const String DefaultPersonalServerNameUpper = "PERSONAL";

        /// <summary>
        /// Name of Administrator application.
        /// </summary>
        public const String AdministratorAppName = "Administrator";

        /// <summary>
        /// Gateway internal system port setting name.
        /// </summary>
        public const String GatewayInternalSystemPortSettingName = "InternalSystemPort";

        /// <summary>
        /// Gateway aggregation port setting name.
        /// </summary>
        public const String GatewayAggregationPortSettingName = "AggregationPort";

        /// <summary>
        /// Name of session cookie.
        /// </summary>
        public const String ScSessionCookieName = "ScSessionCookie";

        /// <summary>
        /// Server directory XML element name in configuration file.
        /// </summary>
        public const String ServerConfigDirName = "server-dir";

        /// <summary>
        /// Empty application name.
        /// </summary>
        public const String EmptyAppName = "NoApp";

        /// <summary>
        /// Server directory parent XML element name.
        /// </summary>
        public const String ServerConfigParentXmlName = "service";

        /// <summary>
        /// HTTP header used for Starcounter error code transfer.
        /// </summary>
        public const String ScErrorCodeHttpHeader = "ScErrorCode";

        /// <summary>
        /// Session cookie length.
        /// </summary>
        public const int ScSessionCookieNameLength = 15;

        /// <summary>
        /// Denotes the type of network protocol.
        /// </summary>
        public enum NetworkProtocolType
        {
            PROTOCOL_HTTP1,
            PROTOCOL_WEBSOCKETS,
            PROTOCOL_HTTP2,
            PROTOCOL_TCP,
            PROTOCOL_UDP,
            PROTOCOL_UNKNOWN,
            PROTOCOL_COUNT
        };

        /// <summary>
        /// WebSocket data types.
        /// </summary>
        public enum WebSocketDataTypes
        {
            WS_OPCODE_TEXT = 1,
            WS_OPCODE_BINARY = 2,
            WS_OPCODE_CLOSE = 8,
            WS_OPCODE_PING = 9
        };

#if !__cplusplus

        // C# code

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RegisteredUriManaged
        {
            public unsafe IntPtr method_space_uri;
            public Int32 handler_id;
            public fixed Byte param_types[MixedCodeConstants.MAX_URI_CALLBACK_PARAMS];
            public Byte num_params;
        };

        public struct UserDelegateParamInfo
        {
            public UInt16 offset_;
            public UInt16 len_;

            public UserDelegateParamInfo(UInt16 offset, UInt16 len)
            {
                offset_ = offset;
                len_ = len;
            }
        }
    };

#endif

#if __cplusplus

	// C++ code

    typedef uint64_t server_log_handle_type;

    struct RegisteredUriManaged
    {
        char* method_space_uri;
        int32_t handler_id;
        uint8_t param_types[MixedCodeConstants::MAX_URI_CALLBACK_PARAMS];
        uint8_t num_params;
    };

    struct UserDelegateParamInfo
    {
        uint16_t offset_;
        uint16_t len_;
    };

    typedef int32_t (*MatchUriType) (char* uri_info, uint32_t uri_info_len, UserDelegateParamInfo** params);

    typedef uint32_t (*GenerateNativeUriMatcherType) (
        server_log_handle_type server_log_handle,
        const char* const root_function_name,
        RegisteredUriManaged* uri_infos,
        uint32_t num_uris,
        char* gen_code_str,
        uint32_t* gen_code_str_num_bytes
        );

#undef public
}
}

#else

    // C# code
}

#endif

