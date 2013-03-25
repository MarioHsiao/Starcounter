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
        /// BMX protocol begin offset.
        /// </summary>
        public const int BMX_PROTOCOL_BEGIN_OFFSET = 16;

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
        /// Offset of gateway data in chunk.
        /// </summary>
        public const int GATEWAY_DATA_BEGIN_OFFSET = BMX_HEADER_MAX_SIZE_BYTES + OVERLAPPED_SIZE;

        /// <summary>
        /// Data offset/size constants. 
        /// </summary>
        public const int BMX_HANDLER_SIZE = 2;

        /// <summary>
        /// Request size begin offset.
        /// </summary>
        public const int REQUEST_SIZE_BEGIN_OFFSET = BMX_PROTOCOL_BEGIN_OFFSET + BMX_HANDLER_SIZE;

        /// <summary>
        /// Gateway session salt offset.
        /// </summary>
        public const int SESSION_SALT_OFFSET = GATEWAY_DATA_BEGIN_OFFSET;

        /// <summary>
        /// Gateway session index offset.
        /// </summary>
        public const int SESSION_INDEX_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + 8;

        /// <summary>
        /// Apps session index offset.
        /// </summary>
        public const int SESSION_APPS_UNIQUE_SESSION_NUMBER_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + 16;

        /// <summary>
        /// Size of the session structure in bytes.
        /// </summary>
        public const int SESSION_STRUCT_SIZE = 24;

        /// <summary>
        /// User data offset in chunk.
        /// </summary>
        public const int USER_DATA_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + SESSION_STRUCT_SIZE;

        /// <summary>
        /// Max user data offset in chunk.
        /// </summary>
        public const int MAX_USER_DATA_BYTES_OFFSET = USER_DATA_OFFSET + 4;

        /// <summary>
        /// User data written bytes offset.
        /// </summary>
        public const int USER_DATA_WRITTEN_BYTES_OFFSET = MAX_USER_DATA_BYTES_OFFSET + 4;

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
        public const int SOCKET_DATA_OFFSET_PARAMS_INFO = 640;

        /// <summary>
        /// Offset of data blob in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_BLOB = 704;

        /// <summary>
        /// HTTP request offset in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = 224;

        /// <summary>
        /// Number of chunks offset in gateway.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_NUM_CHUNKS = 76;

        /// <summary>
        /// Maximum number of URI callback parameters.
        /// </summary>
        public const int MAX_URI_CALLBACK_PARAMS = 16;

        /// <summary>
        /// Shared memory chunk size.
        /// </summary>
        public const int SHM_CHUNK_SIZE = 1 << 12; // 4K chunks.

        /// <summary>
        /// Linked chunk flag.
        /// </summary>
        public const int LINKED_CHUNKS_FLAG = 1;

        /// <summary>
        /// MAX_PREPARSED_HTTP_REQUEST_HEADERS
        /// </summary>
        public const int MAX_PREPARSED_HTTP_REQUEST_HEADERS = 16;

        /// <summary>
        /// MAX_PREPARSED_HTTP_RESPONSE_HEADERS
        /// </summary>
        public const int MAX_PREPARSED_HTTP_RESPONSE_HEADERS = 32;

        // Maximum URI string length.
        public const int MAX_URI_STRING_LEN = 512;

        // Session parameter type number in user delegate.
        public const int REST_ARG_SESSION = 10;

        /// <summary>
        /// Example of string constant.
        /// </summary>
        public const String DefaultPersonalServerNameUpper = "PERSONAL";

        /// <summary>
        /// Name of Administrator application.
        /// </summary>
        public const String AdministratorAppName = "Administrator";

#if !__cplusplus

        // C# code

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RegisteredUriManaged
        {
            public unsafe IntPtr original_uri_info_string;
            public UInt32 original_uri_info_len_chars;
            public unsafe IntPtr processed_uri_info_string;
            public UInt32 processed_uri_info_len_chars;
            public Int32 handler_index;
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

