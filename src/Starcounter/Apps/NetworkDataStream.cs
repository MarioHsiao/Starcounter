
using System;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter
{
	public unsafe struct NetworkDataStream
	{
        // Data offset/size constants.
        public const Int32 BMX_HANDLER_SIZE = 2;
        public const Int32 BMX_PROTOCOL_BEGIN = 16;
        public const Int32 REQUEST_SIZE_BEGIN = BMX_PROTOCOL_BEGIN + BMX_HANDLER_SIZE;
        public const Int32 GATEWAY_CHUNK_BEGIN = 24;
        public const Int32 SESSION_INDEX_OFFSET = 8;
        public const Int32 USER_DATA_OFFSET = 12;
        public const Int32 MAX_USER_DATA_BYTES_OFFSET = 16;
        public const Int32 USER_DATA_WRITTEN_BYTES_OFFSET = 20;

		private Byte* unmanaged_chunk_;
		private Boolean single_chunk_;
		private UInt32 chunk_index_;

		internal NetworkDataStream(
			Byte* unmanagedChunk, 
			Boolean isSingleChunk, 
			UInt32 chunkIndex)
		{
			unmanaged_chunk_ = unmanagedChunk;
			single_chunk_ = isSingleChunk;
			chunk_index_ = chunkIndex;
		}

        public void Init(
            Byte* unmanagedChunk,
            Boolean isSingleChunk,
            UInt32 chunkIndex)
        {
            unmanaged_chunk_ = unmanagedChunk;
            single_chunk_ = isSingleChunk;
            chunk_index_ = chunkIndex;
        }

		public Int32 PayloadSize
		{
			get
			{
                return *((Int32*)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + USER_DATA_WRITTEN_BYTES_OFFSET));
			}
		}

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
                    Int32* userDataOffsetPtr = (Int32*)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + USER_DATA_OFFSET);

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
			if (ec != 0) throw ErrorCode.ToException(ec);
		}

        public void Write(Byte[] buffer, Int32 offset, Int32 length)
		{
            UInt32 ec;

			// TODO:
			// It should be possible to call Write several times and each time 
			// the data is sent to the gateway. 
			// We need someway to tag chunks with needed metadata as well
			// as make sure we have a new chunk or a pointer to an existing chunk.
			fixed (byte* p = buffer)
			{
                UInt32* userDataOffsetPtr = (UInt32*)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + USER_DATA_OFFSET);
                Int32* maxUserDataBytes = (Int32*)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + MAX_USER_DATA_BYTES_OFFSET);
                if (*maxUserDataBytes < length)
                    throw new ArgumentException("Not enough space to write user data.");

                Int32* userDataWrittenBytes = (Int32*)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + USER_DATA_WRITTEN_BYTES_OFFSET);
                *userDataWrittenBytes = length;

                // Setting non-bmx-management chunk type.
                (*(Int16*)(unmanaged_chunk_ + BMX_PROTOCOL_BEGIN)) = Int16.MaxValue;

                // Setting request size to zero.
                (*(UInt32*)(unmanaged_chunk_ + REQUEST_SIZE_BEGIN)) = 0;

                // Copying user data to chunk.
                Marshal.Copy(
                    buffer,
                    offset,
                    (IntPtr)(unmanaged_chunk_ + GATEWAY_CHUNK_BEGIN + *userDataOffsetPtr),
                    length);

                /*
                UInt32 chunk_index = _chunkIndex;
                ec = sccoresrv.sc_bmx_write_to_chunk(
                    p + startIndex,
                    (UInt32)length,
                    &chunk_index,
                    *userDataOffsetPtr
                );
                */
            }
			//if (ec != 0) throw ErrorCode.ToException(ec);

            ec = sccorelib.cm_send_to_client(chunk_index_);
			if (ec != 0) throw ErrorCode.ToException(ec);
		}
	}
}
