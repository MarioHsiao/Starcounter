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
    public unsafe class NetworkDataStream
    {
        /// <summary>
        /// </summary>
        [DllImport("coalmine.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint cm_get_shared_memory_chunk(uint chunk_index, byte** chunk_mem_out);

        /// <summary>
        /// The chunk_index_
        /// </summary>
        UInt32 chunkIndex_ = MixedCodeConstants.INVALID_CHUNK_INDEX;

        /// <summary>
        /// Gateway worker id from which the chunk came.
        /// </summary>
        Byte gwWorkerId_ = 0;

        /// <summary>
        /// Scheduler id.
        /// </summary>
        Byte schedulerId_ = StarcounterEnvironment.InvalidSchedulerId;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte GatewayWorkerId
        {
            get
            {
                return gwWorkerId_;
            }
        }

        /// <summary>
        /// Scheduler id.
        /// </summary>
        internal Byte SchedulerId
        {
            get
            {
                return schedulerId_;
            }
        }

        /// <summary>
        /// Chunk index.
        /// </summary>
        internal UInt32 ChunkIndex
        {
            get
            {
                return chunkIndex_;
            }
        }

        /// <summary>
        /// Gets chunk memory.
        /// </summary>
        /// <returns></returns>
        internal unsafe Byte* GetChunkMemory() {

            // Checking if chunk is valid.
            if (MixedCodeConstants.INVALID_CHUNK_INDEX != chunkIndex_) {

                Byte* chunkMem = null;
                cm_get_shared_memory_chunk(chunkIndex_, &chunkMem);
                return chunkMem;
            }

            return null;
        }

        /// <summary>
        /// Prohibiting default constructor.
        /// </summary>
        internal NetworkDataStream(UInt32 chunkIndex, Byte gwWorkerId, Byte schedulerId) {
            chunkIndex_ = chunkIndex;
            gwWorkerId_ = gwWorkerId;
            schedulerId_ = schedulerId;
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length_bytes">The length in bytes.</param>
        public void SendResponse(Byte[] buffer, Int32 offset, Int32 length_bytes, Response.ConnectionFlags conn_flags) {

            // Checking if already destroyed.
            if (chunkIndex_ == MixedCodeConstants.INVALID_CHUNK_INDEX) {
                throw new ArgumentNullException("Response was already sent.");
            }

            // Running on current Starcounter scheduler.
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

            // Checking if gateway worker id is malicious.
            if (gwWorkerId_ >= StarcounterEnvironment.Gateway.NumberOfWorkers) {

                // Destroying data stream immediately.
                Destroy(true);
                
                return;
            }

            // Processing user data and sending it to gateway.
            UInt32 cur_chunk_index = chunkIndex_;

            // NOTE: We are ignoring error here because we can't do much about it.
            bmx.sc_bmx_send_buffer(gwWorkerId_, p + offset, length_bytes, &cur_chunk_index, (UInt32)conn_flags);

            chunkIndex_ = cur_chunk_index;

            // Destroying data stream immediately.
            Destroy(true);
        }

        /// <summary>
        /// Releases chunk.
        /// </summary>
        void ReleaseChunk()
        {
            // Returning linked chunks to pool.
            UInt32 chunkIndex = chunkIndex_;
            bmx.sc_bmx_release_linked_chunks(&chunkIndex);
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
            // Checking if already destroyed.
            if (IsDestroyed())
                return;

            // Checking if this request is garbage collected.
            if (!isStarcounterThread)
            {
                NetworkDataStream thisInst = this;
                StarcounterBase._DB.RunAsync(() => { thisInst.ReleaseChunk(); });
                return;
            }

            // Request is not garbage collected meaning that we are on Starcounter thread.
            ReleaseChunk();
        }
    }
}
