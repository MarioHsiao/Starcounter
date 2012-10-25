// ***********************************************************************
// <copyright file="NetworkDataStream.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using HttpStructs;
using Starcounter.Internal;

namespace Starcounter
{
    /// <summary>
    /// Struct NetworkDataStream
    /// </summary>
    public unsafe struct NetworkDataStream : INetworkDataStream
    {
        // Data offset/size constants.
        /// <summary>
        /// The BM x_ HANDLE r_ SIZE
        /// </summary>
        public const Int32 BMX_HANDLER_SIZE = 2;
        
        /// <summary>
        /// The BM x_ PROTOCO l_ BEGIN
        /// </summary>
        public const Int32 BMX_PROTOCOL_BEGIN = 16;
        
        /// <summary>
        /// The REQUES t_ SIZ e_ BEGIN
        /// </summary>
        public const Int32 REQUEST_SIZE_BEGIN = BMX_PROTOCOL_BEGIN + BMX_HANDLER_SIZE;
        
        /// <summary>
        /// The GATEWA y_ CHUN k_ BEGIN
        /// </summary>
        public const Int32 GATEWAY_CHUNK_BEGIN = 24;
        
        /// <summary>
        /// The GATEWA y_ DAT a_ BEGIN
        /// </summary>
        public const Int32 GATEWAY_DATA_BEGIN = GATEWAY_CHUNK_BEGIN + 32;
        
        /// <summary>
        /// The SESSIO n_ SAL T_ OFFSET
        /// </summary>
        public const Int32 SESSION_SALT_OFFSET = GATEWAY_DATA_BEGIN;
       
        /// <summary>
        /// The SESSIO n_ INDE x_ OFFSET
        /// </summary>
        public const Int32 SESSION_INDEX_OFFSET = GATEWAY_DATA_BEGIN + 8;
        
        /// <summary>
        /// The SESSIO n_ INDE x_ OFFSET
        /// </summary>
        public const Int32 SESSION_APPS_UNIQUE_SESSION_NUMBER_OFFSET = GATEWAY_DATA_BEGIN + 16;
        
        /// <summary>
        /// The USE r_ DAT a_ OFFSET
        /// </summary>
        public const Int32 USER_DATA_OFFSET = GATEWAY_DATA_BEGIN + 24;
        
        /// <summary>
        /// The MA x_ USE r_ DAT a_ BYTE s_ OFFSET
        /// </summary>
        public const Int32 MAX_USER_DATA_BYTES_OFFSET = GATEWAY_DATA_BEGIN + 28;
        
        /// <summary>
        /// The USE r_ DAT a_ WRITTE n_ BYTE s_ OFFSET
        /// </summary>
        public const Int32 USER_DATA_WRITTEN_BYTES_OFFSET = GATEWAY_DATA_BEGIN + 32;

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
                        (IntPtr)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + *userDataOffsetPtr),
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
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void Write(Byte[] buffer, Int32 offset, Int32 length)
        {
            // TODO:
            // It should be possible to call Write several times and each time 
            // the data is sent to the gateway. 
            // We need someway to tag chunks with needed metadata as well
            // as make sure we have a new chunk or a pointer to an existing chunk.
            fixed (Byte* p = buffer)
            {
                // Processing user data and sending it to gateway.
                UInt32 ec = bmx.sc_bmx_send_buffer(p + offset, (UInt32)length, chunk_index_, unmanaged_chunk_);

                // Checking if any error occurred.
                if (ec != 0)
                    throw ErrorCode.ToException(ec);
            }
        }
    }
}
