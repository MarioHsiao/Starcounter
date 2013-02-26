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
        /// HTTP request offset in socket data.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_HTTP_REQUEST = 232;

        /// <summary>
        /// Number of chunks offset in gateway.
        /// </summary>
        public const int SOCKET_DATA_OFFSET_NUM_CHUNKS = 84;

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
    };

#endif

    // Example of enum definition.
    /*
    public enum ENUM_MY_ENUM
    {
        VALUE_1,
        VALUE_2,
        VALUE_3,
        VALUE_4
    };*/

#if __cplusplus

	// C++ code

#undef public
}
}

#else

    // C# code
}

#endif

