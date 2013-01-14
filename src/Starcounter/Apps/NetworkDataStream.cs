// ***********************************************************************
// <copyright file="NetworkDataStream.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using HttpStructs;
using Starcounter.Internal;
using System.Diagnostics;

namespace Starcounter
{
    /// <summary>
    /// Struct NetworkDataStream
    /// </summary>
    public unsafe struct NetworkDataStream : INetworkDataStream
    {
        /// <summary>
        /// Data offset/size constants. 
        /// </summary>
        public const Int32 BMX_HANDLER_SIZE = 2;

        /// <summary>
        /// BMX protocol begin offset.
        /// </summary>
        public const Int32 BMX_PROTOCOL_BEGIN_OFFSET = 16;
        
        /// <summary>
        /// Request size begin offset.
        /// </summary>
        public const Int32 REQUEST_SIZE_BEGIN_OFFSET = BMX_PROTOCOL_BEGIN_OFFSET + BMX_HANDLER_SIZE;
        
        /// <summary>
        /// BMX header max size.
        /// </summary>
        public const Int32 BMX_HEADER_MAX_SIZE_BYTES = 24;
        
        /// <summary>
        /// Offset of gateway data in chunk.
        /// </summary>
        public const Int32 GATEWAY_DATA_BEGIN_OFFSET = BMX_HEADER_MAX_SIZE_BYTES + 32;
        
        /// <summary>
        /// Gateway session salt offset.
        /// </summary>
        public const Int32 SESSION_SALT_OFFSET = GATEWAY_DATA_BEGIN_OFFSET;
       
        /// <summary>
        /// Gateway session index offset.
        /// </summary>
        public const Int32 SESSION_INDEX_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + 8;
        
        /// <summary>
        /// Apps session index offset.
        /// </summary>
        public const Int32 SESSION_APPS_UNIQUE_SESSION_NUMBER_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + 16;

        /// <summary>
        /// Size of the session structure in bytes.
        /// </summary>
        public const Int32 SESSION_STRUCT_SIZE = 32;

        /// <summary>
        /// User data offset in chunk.
        /// </summary>
        public const Int32 USER_DATA_OFFSET = GATEWAY_DATA_BEGIN_OFFSET + SESSION_STRUCT_SIZE;
        
        /// <summary>
        /// Max user data offset in chunk.
        /// </summary>
        public const Int32 MAX_USER_DATA_BYTES_OFFSET = USER_DATA_OFFSET + 4;
        
        /// <summary>
        /// User data written bytes offset.
        /// </summary>
        public const Int32 USER_DATA_WRITTEN_BYTES_OFFSET = MAX_USER_DATA_BYTES_OFFSET + 4;

        /// <summary>
        /// Invalid chunk index.
        /// </summary>
        const UInt32 INVALID_CHUNK_INDEX = UInt32.MaxValue;

        /// <summary>
        /// The unmanaged_chunk_
        /// </summary>
        Byte* unmanaged_chunk_;

        /// <summary>
        /// The single_chunk_
        /// </summary>
        Boolean single_chunk_;

        /// <summary>
        /// The chunk_index_
        /// </summary>
        UInt32 chunk_index_;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDataStream" /> struct.
        /// </summary>
        /// <param name="unmanagedChunk">The unmanaged chunk.</param>
        /// <param name="isSingleChunk">The is single chunk.</param>
        /// <param name="chunkIndex">Index of the chunk.</param>
        internal NetworkDataStream(
            Byte* unmanagedChunk,
            Boolean isSingleChunk,
            UInt32 chunkIndex)
        {
            unmanaged_chunk_ = unmanagedChunk;
            single_chunk_ = isSingleChunk;
            chunk_index_ = chunkIndex;
        }

        /// <summary>
        /// Inits the specified unmanaged chunk.
        /// </summary>
        /// <param name="unmanagedChunk">The unmanaged chunk.</param>
        /// <param name="isSingleChunk">The is single chunk.</param>
        /// <param name="chunkIndex">Index of the chunk.</param>
        public void Init(
            Byte* unmanagedChunk,
            Boolean isSingleChunk,
            UInt32 chunkIndex)
        {
            unmanaged_chunk_ = unmanagedChunk;
            single_chunk_ = isSingleChunk;
            chunk_index_ = chunkIndex;
        }

        /// <summary>
        /// Gets the size of the payload.
        /// </summary>
        /// <value>The size of the payload.</value>
        public Int32 PayloadSize
        {
            get
            {
                return *((Int32*)(unmanaged_chunk_ + USER_DATA_WRITTEN_BYTES_OFFSET));
            }
        }

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="System.ArgumentNullException">dest</exception>
        /// <exception cref="System.ArgumentException">Not enough free space in destination buffer.</exception>
        public void Read(Byte[] buffer, Int32 offset, Int32 length)
        {
            UInt32 ec;

            if (buffer == null) throw new ArgumentNullException("dest");
            if ((buffer.Length - offset) < length)
                throw new ArgumentException("Not enough free space in destination buffer.");
            if (length > PayloadSize)
                throw new ArgumentException("Specified length is larger than actual size.");

            if (single_chunk_)
            {
                unsafe
                {
                    if (PayloadSize > length)
                        throw new ArgumentException("Not enough space to write user data.");

                    // Reading user data offset.
                    Int32* userDataOffsetPtr = (Int32*)(unmanaged_chunk_ + USER_DATA_OFFSET);

                    // Copying the data to user buffer.
                    Marshal.Copy(
                        (IntPtr)(unmanaged_chunk_ + BMX_HEADER_MAX_SIZE_BYTES + *userDataOffsetPtr),
                        buffer,
                        offset,
                        PayloadSize);
                }

                return;
            }

            // If the unmanaged buffer is linked, i.e. the message is larger
            // than what fits in one chunk, we call a native function that will
            // copy all data into the destination buffer since we don't want to 
            // read linked chunks in managed code.
            fixed (byte* p = buffer)
            {
                ec = bmx.sc_bmx_read_from_chunk(
                        chunk_index_,
                        unmanaged_chunk_,
                        (UInt32)length,
                        p + offset,
                        (UInt32)(buffer.Length - offset)
                );
            }
            if (ec != 0)
                throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Copies scalar bytes from incoming buffer to variable.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public UInt64 ReadUInt64(Int32 offset)
        {
            unsafe
            {
                // Reading user data offset.
                Int32* user_data_offset_ptr = (Int32*)(unmanaged_chunk_ + USER_DATA_OFFSET);

                // Returning scalar value.
                return *(UInt64*)(unmanaged_chunk_ + BMX_HEADER_MAX_SIZE_BYTES + *user_data_offset_ptr + offset);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        public void SendResponse(UInt64[] buffer, Int32 offset, Int32 length_bytes)
        {
            // Checking if already destroyed.
            if (chunk_index_ == INVALID_CHUNK_INDEX)
                return;

            fixed (UInt64* p = buffer)
            {
                SendResponseBufferInternal((Byte*)p, offset, length_bytes);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length_bytes)
        {
            // Checking if already destroyed.
            if (chunk_index_ == INVALID_CHUNK_INDEX)
                return;

            fixed (Byte* p = buffer)
            {
                SendResponseBufferInternal(p, offset, length_bytes);
            }
        }

        /// <summary>
        /// Writes the given buffer.
        /// </summary>
        /// <param name="p">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        unsafe void SendResponseBufferInternal(Byte* p, Int32 offset, Int32 length_bytes)
        {
            // Processing user data and sending it to gateway.
            UInt32 cur_chunk_index = chunk_index_;
            UInt32 ec = bmx.sc_bmx_send_buffer(p + offset, (UInt32)length_bytes, &cur_chunk_index, unmanaged_chunk_);
            chunk_index_ = cur_chunk_index;

            // Checking if any error occurred.
            if (ec != 0)
            {
                Console.WriteLine("Failed to obtain chunk!");
                throw ErrorCode.ToException(ec);
            }
        }

        /// <summary>
        /// Frees all data stream resources like chunks.
        /// </summary>
        public void Destroy()
        {
            // Checking if already destroyed.
            if (chunk_index_ == INVALID_CHUNK_INDEX)
                return;

            // Returning linked chunks to pool.
            UInt32 ec = bmx.sc_bmx_release_linked_chunks(chunk_index_);
            Debug.Assert(ec == 0);

            // This data stream becomes unusable.
            unmanaged_chunk_ = null;
            chunk_index_ = INVALID_CHUNK_INDEX;
        }
    }
}
