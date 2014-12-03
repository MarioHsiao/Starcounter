// ***********************************************************************
// <copyright file="NetworkDataStream.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using Starcounter.Internal;
using System.Diagnostics;
using Starcounter.Advanced;
using System.Collections.Generic;

namespace Starcounter
{
    /// <summary>
    /// Struct NetworkDataStream
    /// </summary>
    public unsafe class NetworkDataStream : Finalizing
    {
        /// <summary>
        /// The unmanaged_chunk_
        /// </summary>
        Byte* rawChunkPtr_ = null;
        
        /// <summary>
        /// Raw chunk data.
        /// </summary>
        internal Byte* RawChunk { get { return rawChunkPtr_; } }

        /// <summary>
        /// The chunk_index_
        /// </summary>
        UInt32 chunkIndex_ = MixedCodeConstants.INVALID_CHUNK_INDEX;

        /// <summary>
        /// Chunk index.
        /// </summary>
        internal UInt32 ChunkIndex { get { return chunkIndex_; } }

        /// <summary>
        /// Gateway worker id from which the chunk came.
        /// </summary>
        Byte gwWorkerId_ = 0;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte GatewayWorkerId { get { return gwWorkerId_; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDataStream" /> struct.
        /// </summary>
        internal void Init(
            Byte* rawChunk,
            UInt32 chunkIndex,
            Byte gwWorkerId,
            Boolean attachFinalizer)
        {
            rawChunkPtr_ = rawChunk;
            chunkIndex_ = chunkIndex;
            gwWorkerId_ = gwWorkerId;

            // Adding finalizer on demand (WebSockets, RawSockets).
            if (attachFinalizer) {
                CreateFinalizer();
            }
        }

        /// <summary>
        /// Destroys the instance of Request.
        /// </summary>
        override internal void DestroyByFinalizer() {
            Destroy(false);
        }

        /// <summary>
        /// Prohibiting default constructor.
        /// </summary>
        internal NetworkDataStream() {
            
        }

        /// <summary>
        /// Gets the size of the payload.
        /// </summary>
        /// <value>The size of the payload.</value>
        public Int32 PayloadSize
        {
            get
            {
                return *((Int32*)(rawChunkPtr_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES));
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

            unsafe
            {
                if (PayloadSize > length)
                    throw new ArgumentException("Not enough space to write user data.");

                // Reading user data offset.
                UInt16* user_data_offset_in_socket_data = (UInt16*)(rawChunkPtr_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                // Copying the data to user buffer.
                Marshal.Copy(
                    new IntPtr(rawChunkPtr_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *user_data_offset_in_socket_data),
                    buffer,
                    offset,
                    PayloadSize);
            }
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
                UInt16* user_data_offset_in_socket_data = (UInt16*)(rawChunkPtr_ + MixedCodeConstants.CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA);

                // Returning scalar value.
                return *(UInt64*)(rawChunkPtr_ + MixedCodeConstants.CHUNK_OFFSET_SOCKET_DATA + *user_data_offset_in_socket_data + offset);
            }
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length_bytes, Response.ConnectionFlags conn_flags)
        {
            // Checking if already destroyed.
            if (chunkIndex_ == MixedCodeConstants.INVALID_CHUNK_INDEX) {
                throw new ArgumentNullException("Response was already sent on this Request!");
            }

            // Running on current Starcounter thread.
            fixed (Byte* p = buffer) {
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
            // Checking if we are actually sending something.
            if (length_bytes <= 0) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, "You are trying to send an empty data.");
            }

            // Processing user data and sending it to gateway.
            UInt32 cur_chunk_index = chunkIndex_;
            UInt32 ec = bmx.sc_bmx_send_buffer(gwWorkerId_, p + offset, length_bytes, &cur_chunk_index, (UInt32)conn_flags);
            chunkIndex_ = cur_chunk_index;

            // Checking if any error occurred.
            if (ec != 0) {
                throw ErrorCode.ToException(ec);
            }

            // Destroying data stream immediately.
            Destroy(true);
        }

        /// <summary>
        /// Releases chunk.
        /// </summary>
        void ReleaseChunk()
        {
            // Returning linked chunks to pool.
            UInt32 ec = bmx.sc_bmx_release_linked_chunks(chunkIndex_);
            Debug.Assert(ec == 0);

            // This data stream becomes unusable.
            rawChunkPtr_ = null;
            chunkIndex_ = MixedCodeConstants.INVALID_CHUNK_INDEX;
        }

        /// <summary>
        /// Checks if data stream is destroyed.
        /// </summary>
        /// <returns></returns>
        public Boolean IsDestroyed()
        {
            // Checking if already destroyed.
            if (chunkIndex_ == MixedCodeConstants.INVALID_CHUNK_INDEX)
                return true;

            return false;
        }

        /// <summary>
        /// Frees all data stream resources like chunks.
        /// </summary>
        public void Destroy(Boolean isStarcounterThread)
        {
            // NOTE: Removing reference for finalizer in order not to finalize twice.
            UnLinkFinalizer();

            // Checking if already destroyed.
            if (chunkIndex_ == MixedCodeConstants.INVALID_CHUNK_INDEX)
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
