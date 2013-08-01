#if __cplusplus

#pragma once

// C++ code--removes public keyword for C++
#define public

typedef char* const String;
typedef unsigned int uint;

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
        /// OVERLAPPED_SIZE
        /// </summary>
        public const int OVERLAPPED_SIZE = 32;

        /// <summary>
        /// Offset of socket data in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_SOCKET_DATA = BMX_HEADER_MAX_SIZE_BYTES;

        /// <summary>
        /// Offset of session in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_SESSION = BMX_HEADER_MAX_SIZE_BYTES + OVERLAPPED_SIZE;

        /// <summary>
        /// Data offset/size constants. 
        /// </summary>
        public const int BMX_HANDLER_SIZE = 2;

        /// <summary>
        /// Chunk session scheduler id offset.
        /// </summary>
        public const int CHUNK_OFFSET_SESSION_SCHEDULER_ID = CHUNK_OFFSET_SESSION;

        /// <summary>
        /// Chunk session linear index offset.
        /// </summary>
        public const int CHUNK_OFFSET_SESSION_LINEAR_INDEX = CHUNK_OFFSET_SESSION_SCHEDULER_ID + 4;

        /// <summary>
        /// Chunk session random salt offset.
        /// </summary>
        public const int CHUNK_OFFSET_SESSION_RANDOM_SALT = CHUNK_OFFSET_SESSION_LINEAR_INDEX + 4;

        /// <summary>
        /// Chunk session view model index offset.
        /// </summary>
        public const int CHUNK_OFFSET_SESSION_RESERVED_INDEX = CHUNK_OFFSET_SESSION_RANDOM_SALT + 8;

        /// <summary>
        /// Session string length in characters.
        /// </summary>
        public const int SESSION_STRING_LEN_CHARS = 32;

        /// <summary>
        /// Session string prefix.
        /// </summary>
        public const int SESSION_STRING_PREFIX_LEN = 8;

        /// <summary>
        /// Session string prefix.
        /// </summary>
        public const int SESSION_STRING_FULL_LEN = SESSION_STRING_LEN_CHARS + SESSION_STRING_PREFIX_LEN;

        /// <summary>
        /// Session string prefix.
        /// </summary>
        public const String SESSION_STRING_PREFIX = "/scsssn/";

        /// <summary>
        /// Size of the session structure in bytes.
        /// </summary>
        public const int SESSION_STRUCT_SIZE = 24;

        /// <summary>
        /// User data offset in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA = 136;

        /// <summary>
        /// Max user data offset in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_MAX_USER_DATA_BYTES = CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA + 4;

        /// <summary>
        /// User data written bytes offset.
        /// </summary>
        public const int CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES = CHUNK_OFFSET_MAX_USER_DATA_BYTES + 4;

        /// <summary>
        /// Fixed handler id offset in chunk.
        /// </summary>
        public const int CHUNK_OFFSET_SAVED_USER_HANDLER_ID = 120;

        /// <summary>
        /// Socket flags.
        /// </summary>
        public const int CHUNK_OFFSET_SOCKET_FLAGS = 160;

        /// <summary>
        /// Client IP offset in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_CLIENT_IP = 96;

        /// <summary>
        /// Just send flag.
        /// </summary>
        public const int SOCKET_DATA_FLAGS_JUST_SEND = 64;

        /// <summary>
        /// Just disconnect flag.
        /// </summary>
        public const int SOCKET_DATA_FLAGS_DISCONNECT = 128;

        /// <summary>
        /// Type of network operation.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_NETWORK_PROTO_TYPE = 141;

        /// <summary>
        /// Socket number.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_SOCKET_NUMBER = 64;

        /// <summary>
        /// Socket number.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_SOCKET_UNIQUE_ID = 72;

        /// <summary>
        /// Port index.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_PORT_INDEX = 132;

        /// <summary>
        /// WebSockets frame opcode.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_WS_OPCODE = 593;

        /// <summary>
        /// Invalid chunk index.
        /// </summary>
        public const uint INVALID_CHUNK_INDEX = 0xFFFFFFFF;

        /// <summary>
        /// Offset in bytes for the session.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_SESSION = 32;

        /// <summary>
        /// Offset in bytes for the session.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_PARAMS_INFO = 632;

        /// <summary>
        /// Offset of data blob in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_BLOB = 696;

        /// <summary>
        /// HTTP request offset in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = 216;

        /// <summary>
        /// Number of chunks offset in gateway.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_NUM_CHUNKS = 124;

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
        public const int SHM_CHUNK_SIZE = 1 << 12; // 4K chunks.

        /// <summary>
        /// Linked chunk flag.
        /// </summary>
        public const int LINKED_CHUNKS_FLAG = 1;

        /// <summary>
        /// Number of clone bytes in socket data.
        /// </summary>
        public const int SOCKET_DATA_NUM_CLONE_BYTES = 144;

        /// <summary>
        /// Number of clone bytes in chunk.
        /// </summary>
        public const int CHUNK_NUM_CLONE_BYTES = CHUNK_OFFSET_SOCKET_DATA + SOCKET_DATA_NUM_CLONE_BYTES;

        // Chunk reserved bytes at the end.
        // TODO: Fix when non null value.
        public const int CHUNK_TAIL_RESERVED_BYTES = 0;

        /// <summary>
        /// Chunk link size.
        /// </summary>
        public const int CHUNK_LINK_SIZE = 8;

        /// <summary>
        /// Chunk data max size.
        /// </summary>
        public const int CHUNK_MAX_DATA_BYTES = SHM_CHUNK_SIZE - CHUNK_LINK_SIZE - CHUNK_TAIL_RESERVED_BYTES;

        /// <summary>
        /// Socket data max size.
        /// </summary>
        public const int SOCKET_DATA_MAX_SIZE = CHUNK_MAX_DATA_BYTES - CHUNK_OFFSET_SOCKET_DATA;

        /// <summary>
        /// MAX_PREPARSED_HTTP_REQUEST_HEADERS
        /// </summary>
        public const int MAX_PREPARSED_HTTP_REQUEST_HEADERS = 16;

        /// <summary>
        /// Size of socket data blob.
        /// </summary>
        public const int SOCKET_DATA_BLOB_SIZE_BYTES = SOCKET_DATA_MAX_SIZE - SOCKET_DATA_OFFSET_BLOB;

        /// <summary>
        /// MAX_PREPARSED_HTTP_RESPONSE_HEADERS
        /// </summary>
        public const int MAX_PREPARSED_HTTP_RESPONSE_HEADERS = 32;

        // Maximum URI string length.
        public const int MAX_URI_STRING_LEN = 512;

        // Session parameter type number in user delegate.
        public const int REST_ARG_SESSION = 10;

        // Bad server log handler.
        public const int INVALID_SERVER_LOG_HANDLE = 0;

        /// <summary>
        /// Example of string constant.
        /// </summary>
        public const String DefaultPersonalServerNameUpper = "PERSONAL";

        /// <summary>
        /// Name of Administrator application.
        /// </summary>
        public const String AdministratorAppName = "Administrator";

        /// <summary>
        /// Denotes the type of network protocol.
        /// </summary>
        public enum NetworkProtocolType
        {
            PROTOCOL_RAW_PORT,
            PROTOCOL_SUB_PORT,
            PROTOCOL_HTTP1,
            PROTOCOL_WEBSOCKETS,
            PROTOCOL_HTTP2
        };

#if !__cplusplus

        // C# code

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RegisteredUriManaged
        {
            public unsafe IntPtr original_uri_info_string;
            public UInt32 original_uri_info_len_chars;
            public unsafe IntPtr processed_uri_info_string;
            public UInt32 processed_uri_info_len_chars;
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
        char* original_uri_info_string;
        uint32_t original_uri_info_len_chars;
        char* processed_uri_info_string;
        uint32_t processed_uri_info_len_chars;
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

