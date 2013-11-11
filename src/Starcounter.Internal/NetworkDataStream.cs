﻿// ***********************************************************************
// <copyright file="NetworkDataStream.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using HttpStructs;
using Starcounter.Internal;
using System.Diagnostics;
using Starcounter.Advanced;

namespace Starcounter
{
    /// <summary>
    /// Struct NetworkDataStream
    /// </summary>
    public unsafe struct NetworkDataStream
    {
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
                return *((Int32*)(unmanaged_chunk_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES));
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
                    UInt16* user_data_offset_in_socket_data = (UInt16*)(unmanaged_chunk_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                    // Copying the data to user buffer.
                    Marshal.Copy(
                        (IntPtr)(unmanaged_chunk_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *user_data_offset_in_socket_data),
                        buffer,
                        offset,
                        PayloadSize);
                }

                return;
            }

            throw new NotImplementedException();
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
                UInt16* user_data_offset_in_socket_data = (UInt16*)(unmanaged_chunk_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                // Returning scalar value.
                return *(UInt64*)(unmanaged_chunk_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *user_data_offset_in_socket_data + offset);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        /// <param name="isStarcounterThread">Is Starcounter thread.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length_bytes, Response.ConnectionFlags conn_flags, Boolean isStarcounterThread)
        {
            // Checking if already destroyed.
            if (chunk_index_ == MixedCodeConstants.INVALID_CHUNK_INDEX)
                throw new ArgumentNullException("Response was already sent on this Request!");

            // Checking if we are not on Starcounter thread now.
            if (!isStarcounterThread)
            {
                NetworkDataStream thisInst = this;

                StarcounterBase._DB.RunSync(() => {
                    fixed (Byte* p = buffer) {
                        thisInst.SendResponseBufferInternal(p, offset, length_bytes, conn_flags);
                    }
                });

                return;
            }

            // Running on current Starcounter thread.
            fixed (Byte* p = buffer)
            {
                SendResponseBufferInternal(p, offset, length_bytes, conn_flags);
            }
        }

        /// <summary>
        /// Writes the given buffer.
        /// </summary>
        /// <param name="p">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        unsafe void SendResponseBufferInternal(Byte* p, Int32 offset, Int32 length_bytes, Response.ConnectionFlags conn_flags)
        {
            // Processing user data and sending it to gateway.
            UInt32 cur_chunk_index = chunk_index_;
            UInt32 ec = bmx.sc_bmx_send_buffer(p + offset, (UInt32)length_bytes, &cur_chunk_index, unmanaged_chunk_, (UInt32)conn_flags);
            chunk_index_ = cur_chunk_index;

            // Checking if any error occurred.
            if (ec != 0)
                throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Releases chunk.
        /// </summary>
        void ReleaseChunk()
        {
            // Returning linked chunks to pool.
            UInt32 ec = bmx.sc_bmx_release_linked_chunks(chunk_index_);
            Debug.Assert(ec == 0);

            // This data stream becomes unusable.
            unmanaged_chunk_ = null;
            chunk_index_ = MixedCodeConstants.INVALID_CHUNK_INDEX;
        }

        /// <summary>
        /// Frees all data stream resources like chunks.
        /// </summary>
        public void Destroy(Boolean isStarcounterThread)
        {
            // Checking if already destroyed.
            if (chunk_index_ == MixedCodeConstants.INVALID_CHUNK_INDEX)
                return;

            // Checking if this request is garbage collected.
            if (!isStarcounterThread)
            {
                NetworkDataStream thisInst = this;
                StarcounterBase._DB.RunSync(() => { thisInst.ReleaseChunk(); });
                return;
            }

            // Request is not garbage collected meaning that we are on Starcounter thread.
            ReleaseChunk();
        }
    }
}
