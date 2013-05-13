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
	if (link_index == smc->link_terminator)
	{
		request_chunk->copy_data_to_buffer(dest_buffer, length);
		return 0;
	}

	// TODO:
	// Read multichunk data.
	return 999;
}

// Obtains new shared memory chunk.
EXTERN_C uint32_t __stdcall sc_bmx_obtain_new_chunk(
    starcounter::core::chunk_index* new_chunk_index,
    uint8_t** new_chunk_mem)
{
    // Obtaining chunk memory.
    return cm_acquire_shared_memory_chunk(new_chunk_index, new_chunk_mem);
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
        memcpy(buffer + cur_offset, chunk_mem, starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
        cur_offset += starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

        // Getting next chunk.
        cur_chunk_index = smc->get_link();
    }
    while (cur_chunk_index != shared_memory_chunk::link_terminator);

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
    int32_t cur_chunk_data_size = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset;
    memcpy(dest_buffer, first_smc + first_chunk_offset, cur_chunk_data_size);
    dest_offset += cur_chunk_data_size;

    // Number of bytes left to copy.
    int32_t bytes_left = total_copy_bytes - cur_chunk_data_size;

    // Next chunk copy size.
    cur_chunk_data_size = bytes_left;
    if (cur_chunk_data_size > starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        cur_chunk_data_size = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

    uint8_t* chunk_mem;
    shared_memory_chunk* smc = (shared_memory_chunk*) first_smc;
    starcounter::core::chunk_index cur_chunk_index = smc->get_link();

    // Until we get the last chunk in chain.
    while (cur_chunk_index != shared_memory_chunk::link_terminator)
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
        if (bytes_left < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
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
    uint32_t* actual_written_bytes,
    uint32_t first_chunk_offset,
    bool just_sending_flag
    )
{
    // Maximum number of bytes that will be written in this call.
    uint32_t num_bytes_to_write = buf_len_bytes;
    uint32_t num_bytes_first_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset;
    
    // Number of chunks to use.
    uint32_t num_extra_chunks_to_use = ((buf_len_bytes - num_bytes_first_chunk) / starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES) + 1;
    assert(num_extra_chunks_to_use > 0);

    // Checking if more than maximum chunks we can take at once.
    if (num_extra_chunks_to_use > starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS)
    {
        num_extra_chunks_to_use = starcounter::bmx::MAX_EXTRA_LINKED_WSABUFS;
        num_bytes_to_write = starcounter::bmx::MAX_BYTES_EXTRA_LINKED_WSABUFS + num_bytes_first_chunk;
    }

    // Acquiring linked chunks.
    uint32_t err_code = cm_acquire_linked_shared_memory_chunks_counted(cur_chunk_index, num_extra_chunks_to_use);
    if (err_code)
    {
        // Releasing the original chunk.
        uint32_t err_code2 = cm_release_linked_shared_memory_chunks(cur_chunk_index);
        assert(err_code2 == 0);

        return err_code;
    }

    // Getting chunk memory address.
    uint8_t* cur_chunk_buf;
    err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
    assert(err_code == 0);

    // Checking if we should just send the chunks.
    if (just_sending_flag)
        (*(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) |= starcounter::MixedCodeConstants::SOCKET_DATA_FLAGS_JUST_SEND;

    // Setting the number of written bytes.
    *(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES) = num_bytes_to_write;

    // Going through each linked chunk and write data there.
    uint32_t left_bytes_to_write = num_bytes_to_write;
    uint32_t num_bytes_to_write_in_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;
    
    // Writing to first chunk.
    memcpy(cur_chunk_buf + first_chunk_offset, buf, num_bytes_first_chunk);
    left_bytes_to_write -= num_bytes_first_chunk;

    // Checking how many bytes to write next time.
    if (left_bytes_to_write < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
    {
        // Checking if we copied everything.
        if (left_bytes_to_write <= 0)
        {
            // Setting number of total written bytes.
            *actual_written_bytes = num_bytes_to_write;

            return 0;
        }

        num_bytes_to_write_in_chunk = left_bytes_to_write;
    }

    // Processing until have some bytes to write.
    while (true)
    {
        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();

        // Getting chunk memory address.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
        assert(err_code == 0);

        // Copying memory.
        memcpy(cur_chunk_buf, buf + num_bytes_to_write - left_bytes_to_write, num_bytes_to_write_in_chunk);
        left_bytes_to_write -= num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (left_bytes_to_write < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        {
            // Checking if we copied everything.
            if (left_bytes_to_write <= 0)
                break;

            num_bytes_to_write_in_chunk = left_bytes_to_write;
        }
    }

    // Setting number of total written bytes.
    *actual_written_bytes = num_bytes_to_write;

    return 0;
}

// Does everything to send the small linear user buffer to the client.
__forceinline uint32_t __stdcall sc_bmx_send_small_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    uint32_t chunk_user_data_offset,
    starcounter::core::chunk_index src_chunk_index,
    uint8_t* src_chunk_buf
    )
{
    // Setting number of actual user bytes written.
    *(uint32_t*)(src_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES) = buf_len_bytes;

    // Copying buffer into chunk.
    memcpy(src_chunk_buf + chunk_user_data_offset, buf, buf_len_bytes);

    // Sending linked chunks to client.
    return cm_send_to_client(src_chunk_index);
}

// Does everything to send the big linear user buffer to the client.
__forceinline uint32_t __stdcall sc_bmx_send_big_buffer(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index src_chunk_index,
    uint32_t first_chunk_offset
    )
{
    starcounter::core::chunk_index new_chunk_index;
    uint32_t last_written_bytes;
    uint32_t err_code;

    uint32_t total_processed_bytes = 0;
    bool just_sending_flag = false;

    // Looping until all user bytes are sent.
    while (true)
    {
        // Copying user data to multiple chunks.
        err_code = sc_bmx_write_to_chunks(
            buf + total_processed_bytes,
            buf_len_bytes - total_processed_bytes,
            src_chunk_index,
            &last_written_bytes,
            first_chunk_offset,
            just_sending_flag
            );

        if (err_code)
            return err_code;

        // Increasing total sent bytes.
        total_processed_bytes += last_written_bytes;

        // Checking if we have processed everything.
        if (total_processed_bytes < buf_len_bytes)
        {
            // Obtaining a new chunk and copying the original chunk header there.
            err_code = sc_bmx_clone_chunk(src_chunk_index, 0, starcounter::MixedCodeConstants::CHUNK_NUM_CLONE_BYTES, &new_chunk_index);
            if (err_code)
                return err_code;

            // Sending linked chunks to client.
            err_code = cm_send_to_client(src_chunk_index);
            assert(err_code == 0);
            //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;

            // Switching to next cloned chunk.
            src_chunk_index = new_chunk_index;

            // The rest of the data is just for sending.
            just_sending_flag = true;
        }
        else
        {
            // Sending linked chunks to client.
            err_code = cm_send_to_client(src_chunk_index);
            assert(err_code == 0);
            //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;

            break;
        }
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
    uint32_t chunk_user_data_offset = *(uint32_t*)(src_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_OFFSET_IN_SOCKET_DATA) +
        starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA;

    uint32_t remaining_bytes_in_orig_chunk = (starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - chunk_user_data_offset);

    // Setting non-bmx-management chunk type.
    (*(BMX_HANDLER_TYPE*)(src_chunk_buf + starcounter::core::chunk_type::bmx_protocol_begin)) = starcounter::bmx::BMX_INVALID_HANDLER_INFO;

    // Setting request size to zero.
    (*(uint32_t*)(src_chunk_buf + starcounter::core::chunk_type::request_size_begin)) = 0;

    uint32_t err_code;

    // Checking if user data fits inside the request chunk.
    if (buf_len_bytes < remaining_bytes_in_orig_chunk)
    {
        // Sending using the same request chunk.
        err_code = sc_bmx_send_small_buffer(buf, buf_len_bytes, chunk_user_data_offset, *src_chunk_index, src_chunk_buf);
        assert(err_code == 0);
    }
    else
    {
        // Sending using multiple linked chunks.
        err_code = sc_bmx_send_big_buffer(buf, buf_len_bytes, *src_chunk_index, chunk_user_data_offset);
    }

    // Chunk becomes unusable.
    *src_chunk_index = shared_memory_chunk::link_terminator;

    return err_code;
}

// Releases given linked chunks.
EXTERN_C uint32_t __stdcall sc_bmx_release_linked_chunks(starcounter::core::chunk_index src_chunk_index)
{
    return cm_release_linked_shared_memory_chunks(src_chunk_index);
}

