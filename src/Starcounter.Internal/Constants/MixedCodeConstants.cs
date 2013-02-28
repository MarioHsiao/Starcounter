#if __cplusplus

#pragma once

// C++ code--removes public keyword for C++
#define public

typedef char* const String;

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
        /// Offset in bytes for the session.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_SESSION = 32;

        /// <summary>
        /// Offset in bytes for the session.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_PARAMS_INFO = 648;

        /// <summary>
        /// Offset of data blob in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_BLOB = 712;

        /// <summary>
        /// HTTP request offset in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = 232;

        /// <summary>
        /// Number of chunks offset in gateway.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_NUM_CHUNKS = 84;

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
        /// Example of string constant.
        /// </summary>
        public const String ThisIsSomeStringConstant = "Hello!";

#if !__cplusplus

        // C# code

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RegisteredUriManaged
        {
            public unsafe IntPtr uri_info_string;
            public UInt32 uri_len_chars;
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
        char* uri_info_string;
        uint32_t uri_len_chars;
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

