
#include "bmx.hpp"

// TODO:
// Add checks for buffer sizes and null pointers.
EXTERN_C uint32_t __stdcall sc_bmx_read_from_chunk(
	uint32_t chunk_index,
	uint8_t* raw_chunk, 
	uint32_t length,
	uint8_t* dest_buffer, 
	uint32_t dest_buffer_size
)
{
	shared_memory_chunk* smc;
	uint32_t request_size;
	uint32_t link_index;
	
	smc = (shared_memory_chunk*)raw_chunk;
	request_chunk_part* request_chunk = smc->get_request_chunk();
	request_size = smc->get_request_size();

	link_index = smc->get_link();
	if (link_index == smc->LINK_TERMINATOR)
	{
		request_chunk->copy_data_to_buffer(dest_buffer, length);
		return 0;
	}

	// TODO:
	// Read multichunk data.
	return 999;
}

// Clones the existing chunk into a new one by copying the specified header.
EXTERN_C __forceinline uint32_t __stdcall sc_bmx_clone_chunk(
    starcounter::core::chunk_index src_chunk_index,
    uint32_t offset,
    uint32_t num_bytes_to_copy,
    starcounter::core::chunk_index* new_chunk_index
    )
{
    uint32_t err_code;

    // Acquiring new chunk.
    err_code = cm_acquire_shared_memory_chunk((DWORD *)new_chunk_index);
    if (err_code)
        return err_code;

    // Getting chunk memory address.
    uint8_t* src_chunk_buf;
    err_code = cm_get_shared_memory_chunk(src_chunk_index, &src_chunk_buf);
    if (err_code)
        return err_code;

    // Getting chunk memory address.
    uint8_t* new_chunk_buf;
    err_code = cm_get_shared_memory_chunk(*new_chunk_index, &new_chunk_buf);
    if (err_code)
        return err_code;

    // Copying memory from original chunk to copy.
    memcpy(new_chunk_buf, src_chunk_buf + offset, num_bytes_to_copy);

    return 0;
}

// Writes given big linear buffer into obtained linked chunks.
EXTERN_C __forceinline uint32_t __stdcall sc_bmx_write_to_chunks(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index cur_chunk_index,
    uint32_t* actual_written_bytes
    )
{
    // Maximum number of bytes that will be written in this call.
    uint32_t num_bytes_to_write = buf_len_bytes;
    
    // Number of chunks to use.
    uint32_t num_chunks_to_use = (buf_len_bytes / starcounter::bmx::MAX_DATA_IN_CHUNK) + 1;

    // Checking if more than maximum chunks we can take at once.
    if (num_chunks_to_use > starcounter::bmx::MAX_WSABUFS_LINKED)
    {
        num_chunks_to_use = starcounter::bmx::MAX_WSABUFS_LINKED;
        num_bytes_to_write = starcounter::bmx::MAX_LINKED_CHUNKS_BYTES;
    }

    // Acquiring linked chunks.
    uint32_t err_code = cm_acquire_linked_shared_memory_chunks_counted(cur_chunk_index, num_chunks_to_use);
    if (err_code)
        return err_code;

    // Getting chunk memory address.
    uint8_t* cur_chunk_buf;
    err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
    if (err_code)
        return err_code;

    // Setting the number of written bytes.
    *(uint32_t*)(cur_chunk_buf + starcounter::bmx::USER_DATA_WRITTEN_BYTES_OFFSET) = num_bytes_to_write;

    // Going through each linked chunk and write data there.
    uint32_t left_bytes_to_write = num_bytes_to_write;
    uint32_t num_bytes_to_write_in_chunk = left_bytes_to_write;
    if (num_bytes_to_write_in_chunk > starcounter::bmx::MAX_DATA_IN_CHUNK)
        num_bytes_to_write_in_chunk = starcounter::bmx::MAX_DATA_IN_CHUNK;

    // Getting index of the first data chunk in chain.
    cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();

    // Processing until have some bytes to write.
    while(left_bytes_to_write > 0)
    {
        // Getting chunk memory address.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
        if (err_code)
            return err_code;
        
        // Copying memory.
        memcpy(cur_chunk_buf, buf + num_bytes_to_write - left_bytes_to_write, num_bytes_to_write_in_chunk);
        left_bytes_to_write -= num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (left_bytes_to_write < starcounter::bmx::MAX_DATA_IN_CHUNK)
            num_bytes_to_write_in_chunk = left_bytes_to_write;

        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();
    }

    // Setting number of total written bytes.
    *actual_written_bytes = num_bytes_to_write;

    return 0;
}

// Does everything to send the small linear user buffer to the client.
EXTERN_C __forceinline uint32_t __stdcall sc_bmx_send_small_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    uint32_t user_data_offset,
    starcounter::core::chunk_index chunk_index,
    uint8_t* chunk_memory
    )
{
    // Setting number of actual user bytes written.
    *(uint32_t*)(chunk_memory + starcounter::bmx::USER_DATA_WRITTEN_BYTES_OFFSET) = buf_len_bytes;

    // Copying buffer into chunk.
    memcpy(chunk_memory + starcounter::bmx::GATEWAY_CHUNK_BEGIN + user_data_offset, buf, buf_len_bytes);

    // Sending linked chunks to client.
    return cm_send_to_client(chunk_index);
}

// Does everything to send the big linear user buffer to the client.
EXTERN_C __forceinline uint32_t __stdcall sc_bmx_send_big_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index src_chunk_index
    )
{
    uint32_t last_written_bytes;
    uint32_t err_code;
    starcounter::core::chunk_index new_chunk_index = shared_memory_chunk::LINK_TERMINATOR;

    // Looping until all user bytes are sent.
    uint32_t total_sent_bytes = 0;
    while (total_sent_bytes < buf_len_bytes)
    {
        // Copying user data to multiple chunks.
        err_code = sc_bmx_write_to_chunks(
            buf + total_sent_bytes,
            buf_len_bytes - total_sent_bytes,
            src_chunk_index,
            &last_written_bytes
            );

        if (err_code)
            return err_code;

        // Increasing total sent bytes.
        total_sent_bytes += last_written_bytes;

        // Checking if not all user data has been copied so we need to make a chunk clone.
        if (total_sent_bytes < buf_len_bytes)
        {
            // Obtaining a new chunk and copying the original chunk header there.
            err_code = sc_bmx_clone_chunk(src_chunk_index, 0, starcounter::bmx::SOCKET_DATA_NUM_CLONE_BYTES, &new_chunk_index);
            if (err_code)
                return err_code;
        }

        // Sending linked chunks to client.
        err_code = cm_send_to_client(src_chunk_index);
        if (err_code)
            return err_code;

        //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;

        // Assigning new source chunk.
        src_chunk_index = new_chunk_index;
    }

    return 0;
}

// The entry point to send any-size user data to client.
EXTERN_C uint32_t __stdcall sc_bmx_send_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index chunk_index,
    uint8_t* chunk_memory
    )
{
    // Points to user data offset in chunk.
    uint32_t user_data_offset = *(uint32_t*)(chunk_memory + starcounter::bmx::USER_DATA_OFFSET);

    // Setting non-bmx-management chunk type.
    (*(int16_t*)(chunk_memory + starcounter::bmx::BMX_PROTOCOL_BEGIN)) = 32767;

    // Setting request size to zero.
    (*(uint32_t*)(chunk_memory + starcounter::bmx::REQUEST_SIZE_BEGIN)) = 0;

    // Checking if user data fits inside the request chunk.
    if (buf_len_bytes < (starcounter::bmx::GATEWAY_ORIG_CHUNK_DATA_SIZE - user_data_offset))
    {
        // Sending using the same request chunk.
        return sc_bmx_send_small_buffer(buf, buf_len_bytes, user_data_offset, chunk_index, chunk_memory);
    }
    else
    {
        // Sending using multiple linked chunks.
        return sc_bmx_send_big_buffer(buf, buf_len_bytes, chunk_index);
    }
}