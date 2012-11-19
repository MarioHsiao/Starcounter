
#include "bmx.hpp"

// TODO:
// Add checks for buffer sizes and null pointers.
EXTERN_C uint32_t __stdcall sc_bmx_read_from_chunk(
	starcounter::core::chunk_index chunk_index,
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

// Writing linked chunks data to a given buffer and releasing all chunks except first.
EXTERN_C uint32_t __stdcall sc_bmx_plain_copy_and_release_chunks(
    starcounter::core::chunk_index first_chunk_index,
    uint8_t* first_chunk_data,
    uint8_t* buffer
    )
{
    uint32_t err_code;
    int32_t cur_offset = 0;

    uint8_t* chunk_mem;
    shared_memory_chunk* smc = (shared_memory_chunk*) first_chunk_data;
    starcounter::core::chunk_index cur_chunk_index = first_chunk_index;

    do
    {
        // Obtaining chunk memory.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &chunk_mem);
        assert(err_code == 0);

        smc = (shared_memory_chunk*) chunk_mem;

        // Copying the whole chunk data.
        memcpy(buffer + cur_offset, chunk_mem, starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK);
        cur_offset += starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

        // Getting next chunk.
        cur_chunk_index = smc->get_link();
    }
    while (cur_chunk_index != shared_memory_chunk::LINK_TERMINATOR);

    // Returning all linked chunks to private/shared pool.
    shared_memory_chunk* first_smc = ((shared_memory_chunk*) first_chunk_data);
    err_code = cm_release_linked_shared_memory_chunks(first_smc->get_link());
    if (err_code)
        return err_code;

    // Terminating the first chunk.
    first_smc->terminate_link();

    return 0;
}

// Writing all chunks data to given buffer.
EXTERN_C uint32_t __stdcall sc_bmx_copy_all_chunks(
    starcounter::core::chunk_index chunk_index,
    uint8_t* first_smc,
    uint32_t first_chunk_offset,
    uint32_t total_copy_bytes,
    uint8_t* dest_buffer,
    uint32_t dest_buffer_size
    )
{
    uint32_t err_code;
    int32_t dest_offset = 0;

    // Copying first chunk data.
    int32_t cur_chunk_data_size = starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK - first_chunk_offset;
    memcpy(dest_buffer, first_smc + first_chunk_offset, cur_chunk_data_size);
    dest_offset += cur_chunk_data_size;

    // Number of bytes left to copy.
    int32_t bytes_left = total_copy_bytes - cur_chunk_data_size;

    // Next chunk copy size.
    cur_chunk_data_size = bytes_left;
    if (cur_chunk_data_size > starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
        cur_chunk_data_size = starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

    uint8_t* chunk_mem;
    shared_memory_chunk* smc = (shared_memory_chunk*) first_smc;
    starcounter::core::chunk_index cur_chunk_index = smc->get_link();

    // Until we get the last chunk in chain.
    while (cur_chunk_index != shared_memory_chunk::LINK_TERMINATOR)
    {
        // Obtaining chunk memory.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &chunk_mem);
        assert(err_code == 0);

        smc = (shared_memory_chunk*) chunk_mem;

        // Copying.
        memcpy(dest_buffer + dest_offset, chunk_mem, cur_chunk_data_size);
        dest_offset += cur_chunk_data_size;

        // Decreasing number of bytes left to be processed.
        bytes_left -= cur_chunk_data_size;
        if (bytes_left < starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
            cur_chunk_data_size = bytes_left;

        // Getting next chunk in chain.
        cur_chunk_index = smc->get_link();
    }

    // Returning all additional chunks to private pool: 
    err_code = cm_release_linked_shared_memory_chunks(((shared_memory_chunk*) first_smc)->get_link());
    if (err_code)
        return err_code;

    return 0;
}

// Clones the existing chunk into a new one by copying the specified header.
__forceinline uint32_t __stdcall sc_bmx_clone_chunk(
    starcounter::core::chunk_index src_chunk_index,
    uint32_t offset,
    uint32_t num_bytes_to_copy,
    starcounter::core::chunk_index* new_chunk_index
    )
{
    uint32_t err_code;

    // Acquiring new chunk.
    uint8_t* new_chunk_buf;
    err_code = cm_acquire_shared_memory_chunk(new_chunk_index, &new_chunk_buf);
    if (err_code)
        return err_code;

    // Getting chunk memory address.
    uint8_t* src_chunk_buf;
    err_code = cm_get_shared_memory_chunk(src_chunk_index, &src_chunk_buf);
    assert(err_code == 0);

    // Copying memory from original chunk to copy.
    memcpy(new_chunk_buf, src_chunk_buf + offset, num_bytes_to_copy);

    return 0;
}

// Writes given big linear buffer into obtained linked chunks.
__forceinline uint32_t __stdcall sc_bmx_write_to_chunks(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index cur_chunk_index,
    uint32_t* actual_written_bytes
    )
{
    // Maximum number of bytes that will be written in this call.
    uint32_t num_bytes_to_write = buf_len_bytes;
    
    // Number of chunks to use.
    uint32_t num_chunks_to_use = (buf_len_bytes / starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK) + 1;

    // Checking if more than maximum chunks we can take at once.
    if (num_chunks_to_use > starcounter::bmx::MAX_NUM_LINKED_WSABUFS)
    {
        num_chunks_to_use = starcounter::bmx::MAX_NUM_LINKED_WSABUFS;
        num_bytes_to_write = starcounter::bmx::MAX_BYTES_LINKED_CHUNKS;
    }

    // Acquiring linked chunks.
    uint32_t err_code = cm_acquire_linked_shared_memory_chunks_counted(cur_chunk_index, num_chunks_to_use);
    if (err_code)
    {
        // Releasing the cloned chunk.
        uint32_t err_code2 = cm_release_linked_shared_memory_chunks(cur_chunk_index);
        assert(err_code2 == 0);

        return err_code;
    }

    // Getting chunk memory address.
    uint8_t* cur_chunk_buf;
    err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
    assert(err_code == 0);

    // Setting the number of written bytes.
    *(uint32_t*)(cur_chunk_buf + starcounter::bmx::USER_DATA_WRITTEN_BYTES_OFFSET) = num_bytes_to_write;

    // Going through each linked chunk and write data there.
    uint32_t left_bytes_to_write = num_bytes_to_write;
    uint32_t num_bytes_to_write_in_chunk = left_bytes_to_write;
    if (num_bytes_to_write_in_chunk > starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
        num_bytes_to_write_in_chunk = starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK;

    // Getting index of the first data chunk in chain.
    cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();

    // Processing until have some bytes to write.
    while(left_bytes_to_write > 0)
    {
        // Getting chunk memory address.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
        assert(err_code == 0);
        
        // Copying memory.
        memcpy(cur_chunk_buf, buf + num_bytes_to_write - left_bytes_to_write, num_bytes_to_write_in_chunk);
        left_bytes_to_write -= num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (left_bytes_to_write < starcounter::bmx::MAX_DATA_BYTES_IN_CHUNK)
            num_bytes_to_write_in_chunk = left_bytes_to_write;

        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();
    }

    // Setting number of total written bytes.
    *actual_written_bytes = num_bytes_to_write;

    return 0;
}

// Does everything to send the small linear user buffer to the client.
__forceinline uint32_t __stdcall sc_bmx_send_small_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    uint32_t user_data_offset,
    starcounter::core::chunk_index src_chunk_index,
    uint8_t* src_chunk_buf
    )
{
    // Setting number of actual user bytes written.
    *(uint32_t*)(src_chunk_buf + starcounter::bmx::USER_DATA_WRITTEN_BYTES_OFFSET) = buf_len_bytes;

    // Copying buffer into chunk.
    memcpy(src_chunk_buf + starcounter::bmx::BMX_HEADER_MAX_SIZE_BYTES + user_data_offset, buf, buf_len_bytes);

    // Sending linked chunks to client.
    return cm_send_to_client(src_chunk_index);
}

// Does everything to send the big linear user buffer to the client.
__forceinline uint32_t __stdcall sc_bmx_send_big_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index src_chunk_index
    )
{
    uint32_t last_written_bytes;
    uint32_t err_code;
    starcounter::core::chunk_index new_chunk_index;

    // Looping until all user bytes are sent.
    uint32_t total_sent_bytes = 0;
    while (total_sent_bytes < buf_len_bytes)
    {
        // Obtaining a new chunk and copying the original chunk header there.
        err_code = sc_bmx_clone_chunk(src_chunk_index, 0, starcounter::bmx::BMX_NUM_CLONE_BYTES, &new_chunk_index);
        if (err_code)
            return err_code;

        // Copying user data to multiple chunks.
        err_code = sc_bmx_write_to_chunks(
            buf + total_sent_bytes,
            buf_len_bytes - total_sent_bytes,
            new_chunk_index,
            &last_written_bytes
            );

        if (err_code)
            return err_code;

        // Increasing total sent bytes.
        total_sent_bytes += last_written_bytes;

        // Sending linked chunks to client.
        err_code = cm_send_to_client(new_chunk_index);
        assert(err_code == 0);

        // NOTE: If not asserting then we need to release new_chunk_index linked chunks.

        //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;
    }

    return 0;
}

// The entry point to send any-size user data to client.
EXTERN_C uint32_t __stdcall sc_bmx_send_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index* src_chunk_index,
    uint8_t* src_chunk_buf
    )
{
    // Points to user data offset in chunk.
    uint32_t user_data_offset = *(uint32_t*)(src_chunk_buf + starcounter::bmx::USER_DATA_OFFSET);

    // Setting non-bmx-management chunk type.
    (*(int16_t*)(src_chunk_buf + starcounter::bmx::BMX_PROTOCOL_BEGIN_OFFSET)) = 0x7FFF;

    // Setting request size to zero.
    (*(uint32_t*)(src_chunk_buf + starcounter::bmx::REQUEST_SIZE_BEGIN)) = 0;

    uint32_t err_code;

    // Checking if user data fits inside the request chunk.
    if (buf_len_bytes < (starcounter::bmx::GATEWAY_ORIG_CHUNK_DATA_SIZE - user_data_offset))
    {
        // Sending using the same request chunk.
        err_code = sc_bmx_send_small_buffer(buf, buf_len_bytes, user_data_offset, *src_chunk_index, src_chunk_buf);
        assert(err_code == 0);

        // Chunk becomes unusable.
        *src_chunk_index = shared_memory_chunk::LINK_TERMINATOR;

        return err_code;
    }
    else
    {
        // Sending using multiple linked chunks.
        err_code = sc_bmx_send_big_buffer(buf, buf_len_bytes, *src_chunk_index);
        if (err_code)
        {
            // When there is an error initial chunk can still be used to send error response etc.
            return err_code;
        }

        // Releasing source chunk back to pool.
        err_code = cm_release_linked_shared_memory_chunks(*src_chunk_index);
        assert(err_code == 0);

        // Chunk can't be used anymore.
        *src_chunk_index = shared_memory_chunk::LINK_TERMINATOR;

        return 0;
    }
}

// Releases given linked chunks.
EXTERN_C uint32_t __stdcall sc_bmx_release_linked_chunks(starcounter::core::chunk_index src_chunk_index)
{
    return cm_release_linked_shared_memory_chunks(src_chunk_index);
}
