#include "bmx.hpp"

namespace starcounter
{
namespace bmx
{

// Obtains new shared memory chunk.
EXTERN_C uint32_t __stdcall sc_bmx_obtain_new_chunk(
    starcounter::core::chunk_index* new_chunk_index,
    uint8_t** new_chunk_mem)
{
    // Obtaining chunk memory.
    return cm_acquire_shared_memory_chunk(new_chunk_index, new_chunk_mem);
}

uint32_t sc_clone_linked_chunks(
    const starcounter::core::chunk_index first_chunk_index, 
    starcounter::core::chunk_index* out_chunk_index) {

    uint32_t err_code;
    *out_chunk_index = shared_memory_chunk::link_terminator;

    uint8_t* src_chunk_mem;
    shared_memory_chunk* src_smc = NULL;
    shared_memory_chunk* dest_smc = NULL;
    starcounter::core::chunk_index src_chunk_index = first_chunk_index;
    starcounter::core::chunk_index dest_chunk_index = shared_memory_chunk::link_terminator;

    do {

        // Obtaining chunk memory.
        err_code = cm_get_shared_memory_chunk(src_chunk_index, &src_chunk_mem);
        _SC_ASSERT(err_code == 0);

        src_smc = (shared_memory_chunk*) src_chunk_mem;

        // Acquiring new chunk.
        uint8_t* dest_chunk_mem;
        err_code = cm_acquire_shared_memory_chunk(&dest_chunk_index, &dest_chunk_mem);
        _SC_ASSERT(0 == err_code);

        if (err_code) {

            if (dest_smc != NULL) {

                cm_release_linked_shared_memory_chunks(*out_chunk_index);
                *out_chunk_index = shared_memory_chunk::link_terminator;
                return err_code;
            }
        }

        if (dest_smc)
            dest_smc->set_link(dest_chunk_index);
        else
            *out_chunk_index = dest_chunk_index;

        dest_smc = (shared_memory_chunk*) dest_chunk_mem;

        // Copying memory from original chunk to copy.
        memcpy(dest_chunk_mem, src_chunk_mem, starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);

        // Getting next chunk.
        src_chunk_index = src_smc->get_link();

    } while (src_chunk_index != shared_memory_chunk::link_terminator);

    return 0;
}

// Writing linked chunks data to a given buffer and releasing all chunks except first.
EXTERN_C void __stdcall sc_bmx_plain_copy_and_release_chunks(
    const starcounter::core::chunk_index first_chunk_index,
    uint8_t* const buffer,
    const int32_t buf_len)
{
    uint32_t err_code;
    int32_t cur_offset = 0;

    uint8_t* chunk_mem;
    starcounter::core::chunk_index cur_chunk_index = first_chunk_index;

    do
    {
        // Obtaining chunk memory.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &chunk_mem);
        _SC_ASSERT(err_code == 0);

        // Copying the whole chunk data.
        memcpy(buffer + cur_offset, chunk_mem, starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
        cur_offset += starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;
        _SC_ASSERT(cur_offset <= buf_len);

        // Getting next chunk.
        cur_chunk_index = ((shared_memory_chunk*) chunk_mem)->get_link();
    }
    while (cur_chunk_index != shared_memory_chunk::link_terminator);

    // Returning all linked chunks (except first one) to private pool.
    err_code = cm_get_shared_memory_chunk(first_chunk_index, &chunk_mem);
    _SC_ASSERT(0 == err_code);

    shared_memory_chunk* first_smc = (shared_memory_chunk*) chunk_mem;
    err_code = cm_release_linked_shared_memory_chunks(first_smc->get_link());
    _SC_ASSERT(0 == err_code);

    // Terminating the first chunk.
    first_smc->terminate_link();
}

// Writing all chunks data to given buffer.
EXTERN_C uint32_t __stdcall sc_bmx_copy_from_chunks_and_release_trailing(
    const starcounter::core::chunk_index first_chunk_index,
    const uint32_t first_chunk_offset,
    const int32_t total_copy_bytes,
    uint8_t* const dest_buffer,
    const int32_t dest_buffer_size
    )
{
    // Copying first chunk data.
    int32_t cur_chunk_num_copy_bytes = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset;
    if (cur_chunk_num_copy_bytes > total_copy_bytes) {
        cur_chunk_num_copy_bytes = total_copy_bytes;
    }

    uint8_t* first_chunk_mem;
    uint32_t err_code = cm_get_shared_memory_chunk(first_chunk_index, &first_chunk_mem);
    _SC_ASSERT(err_code == 0);

    memcpy(dest_buffer, first_chunk_mem + first_chunk_offset, cur_chunk_num_copy_bytes);

    int32_t dest_offset = 0;
    dest_offset += cur_chunk_num_copy_bytes;

    // Number of bytes left to copy.
    int32_t bytes_left = total_copy_bytes - cur_chunk_num_copy_bytes;
    _SC_ASSERT(bytes_left >= 0);

    // Next chunk copy size.
    cur_chunk_num_copy_bytes = bytes_left;
    if (cur_chunk_num_copy_bytes > starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        cur_chunk_num_copy_bytes = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;

    uint8_t* cur_chunk_mem;
    starcounter::core::chunk_index first_linked_chunk_index = ((shared_memory_chunk*)first_chunk_mem)->get_link();
    starcounter::core::chunk_index cur_chunk_index = first_linked_chunk_index;

    // Checking if we had linked chunks.
    if (first_linked_chunk_index != shared_memory_chunk::link_terminator) {

        // Until we get the last chunk in chain.
        while (cur_chunk_index != shared_memory_chunk::link_terminator)
        {
            // Obtaining chunk memory.
            err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_mem);
            _SC_ASSERT(err_code == 0);

            // Copying.
            memcpy(dest_buffer + dest_offset, cur_chunk_mem, cur_chunk_num_copy_bytes);
            dest_offset += cur_chunk_num_copy_bytes;
            _SC_ASSERT(dest_offset <= total_copy_bytes);

            // Decreasing number of bytes left to be processed.
            bytes_left -= cur_chunk_num_copy_bytes;
            _SC_ASSERT(bytes_left >= 0);

            // Checking if no more bytes to copy.
            if (bytes_left == 0) {
                break;
            }

            // Checking if we have less than full chunk to copy.
            if (bytes_left < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
                cur_chunk_num_copy_bytes = bytes_left;

            // Getting next chunk in chain.
            cur_chunk_index = ((shared_memory_chunk*) cur_chunk_mem)->get_link();
        }

        // Returning all additional chunks to private pool: 
        err_code = cm_release_linked_shared_memory_chunks(first_linked_chunk_index);
        if (err_code)
            return err_code;

        // Terminating the first chunk.
        ((shared_memory_chunk*) first_chunk_mem)->terminate_link();
    }

    return 0;
}

// Clones the existing chunk into a new one by copying the specified header.
uint32_t __stdcall sc_bmx_clone_chunk(
    const starcounter::core::chunk_index src_chunk_index,
    const int32_t offset,
    const int32_t num_bytes_to_copy,
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
    _SC_ASSERT(err_code == 0);

    // Copying memory from original chunk to copy.
    memcpy(new_chunk_buf, src_chunk_buf + offset, num_bytes_to_copy);

    return 0;
}

// Writes given big linear buffer into obtained linked chunks.
uint32_t __stdcall sc_bmx_write_to_chunks(
    uint8_t* const buf,
    const int32_t buf_len_bytes,
    const starcounter::core::chunk_index the_chunk_index,
    int32_t* actual_written_bytes,
    const int32_t first_chunk_offset,
    bool just_sending_flag
    )
{
    // Maximum number of bytes that will be written in this call.
    int32_t num_bytes_left_first_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset;
    _SC_ASSERT(num_bytes_left_first_chunk > 0);

    starcounter::core::chunk_index cur_chunk_index = the_chunk_index;

    // Getting chunk memory address.
    uint8_t* cur_smc;
    uint32_t err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_smc);
    _SC_ASSERT(err_code == 0);

    // Checking if we should just send the chunks.
	if (just_sending_flag) {
		(*(uint32_t*)(cur_smc + starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) |= starcounter::MixedCodeConstants::SOCKET_DATA_FLAGS_JUST_SEND;
	}

    // Checking if this chunk was aggregated.
    bool aggregated_flag =
        (0 != ((*(uint32_t*)(cur_smc + starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) & starcounter::MixedCodeConstants::SOCKET_DATA_FLAGS_AGGREGATED));

    // Checking if data fits in one chunk.
    if (buf_len_bytes <= num_bytes_left_first_chunk)
    {
        // Writing to first chunk.
        _SC_ASSERT(first_chunk_offset < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
        _SC_ASSERT(buf_len_bytes < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
        memcpy(cur_smc + first_chunk_offset, buf, buf_len_bytes);

        // Setting the number of written bytes.
        *(uint32_t*)(cur_smc + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_NUM_BYTES) = buf_len_bytes;

        // Setting number of total written bytes.
        *actual_written_bytes = buf_len_bytes;

        return 0;
    }
    
    // Number of chunks to use.
    int32_t num_extra_chunks = ((buf_len_bytes - num_bytes_left_first_chunk) / starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES) + 1;
    _SC_ASSERT(num_extra_chunks > 0);

    int32_t num_data_bytes_for_round = buf_len_bytes;

    // Checking if more than maximum chunks we can take at once.
    if (num_extra_chunks > starcounter::MixedCodeConstants::MAX_EXTRA_LINKED_IPC_CHUNKS)
    {
        _SC_ASSERT(!aggregated_flag);
        num_extra_chunks = starcounter::MixedCodeConstants::MAX_EXTRA_LINKED_IPC_CHUNKS;
        num_data_bytes_for_round = starcounter::MixedCodeConstants::MAX_BYTES_EXTRA_LINKED_IPC_CHUNKS + num_bytes_left_first_chunk;
    }

    // Acquiring linked chunks.
    err_code = cm_acquire_linked_shared_memory_chunks_counted(cur_chunk_index, num_extra_chunks);
    if (err_code) {
        return err_code;
    }

    // Setting the number of written bytes.
    *(uint32_t*)(cur_smc + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_NUM_BYTES) = num_data_bytes_for_round;

    // Going through each linked chunk and write data there.
    int32_t num_bytes_to_write_in_chunk = starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES;
    
    // Writing to first chunk.
    _SC_ASSERT(first_chunk_offset < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
    _SC_ASSERT(num_bytes_left_first_chunk < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
    memcpy(cur_smc + first_chunk_offset, buf, num_bytes_left_first_chunk);

    int32_t cur_data_offset = num_bytes_left_first_chunk;

    // Checking how many bytes to write next time.
    if (num_data_bytes_for_round - cur_data_offset < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES) {
        num_bytes_to_write_in_chunk = num_data_bytes_for_round - cur_data_offset;
    }

    // Processing until have some bytes to write.
    while (true)
    {
        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_smc)->get_link();
        _SC_ASSERT(cur_chunk_index != shared_memory_chunk::link_terminator);

        // Getting chunk memory address.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_smc);
        _SC_ASSERT(err_code == 0);

        // Copying memory.
        _SC_ASSERT(num_bytes_to_write_in_chunk > 0 && num_bytes_to_write_in_chunk <= starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);
        memcpy(cur_smc, buf + cur_data_offset, num_bytes_to_write_in_chunk);
        cur_data_offset += num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (num_data_bytes_for_round - cur_data_offset < starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES)
        {
            // Checking if we copied everything.
            if (num_data_bytes_for_round == cur_data_offset)
                break;

            num_bytes_to_write_in_chunk = num_data_bytes_for_round - cur_data_offset;
        }
    }

    _SC_ASSERT(cur_data_offset == num_data_bytes_for_round);

    // Setting number of total written bytes.
    *actual_written_bytes = num_data_bytes_for_round;

    return 0;
}

// Does everything to send the small linear user buffer to the client.
uint32_t __stdcall sc_bmx_send_small_buffer(
    const uint8_t gw_worker_id,
    uint8_t* const buf,
    const int32_t buf_len_bytes,
    starcounter::core::chunk_index& the_chunk_index,
    const int32_t chunk_user_data_offset    
    )
{
    uint8_t* cur_chunk_buf;
    uint32_t err_code = cm_get_shared_memory_chunk(the_chunk_index, &cur_chunk_buf);
    _SC_ASSERT(err_code == 0);

    // Setting number of actual user bytes written.
    *(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_NUM_BYTES) = buf_len_bytes;

    _SC_ASSERT(chunk_user_data_offset + buf_len_bytes <= starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES);

    // Copying buffer into chunk.
    memcpy(cur_chunk_buf + chunk_user_data_offset, buf, buf_len_bytes);

    shared_memory_chunk* smc = (shared_memory_chunk*)cur_chunk_buf;
    _SC_ASSERT(smc->is_terminated());

    // Sending linked chunks to client.
    err_code = cm_send_to_client(gw_worker_id, the_chunk_index);
    _SC_ASSERT(err_code == 0);

    the_chunk_index = shared_memory_chunk::link_terminator;

    return 0;
}

// Does everything to send the big linear user buffer to the client.
uint32_t __stdcall sc_bmx_send_big_buffer(
    const uint8_t gw_worker_id,
    uint8_t* const buf,
    const int32_t buf_len_bytes,
    starcounter::core::chunk_index& the_chunk_index,
    const int32_t first_chunk_offset
    )
{
    int32_t last_written_bytes = 0;
    uint32_t err_code;

    int32_t total_processed_bytes = 0;
    bool just_sending_flag = false;

    // Looping until all user bytes are sent.
    while (true)
    {
        // Copying user data to multiple chunks.
        err_code = sc_bmx_write_to_chunks(
            buf + total_processed_bytes,
            buf_len_bytes - total_processed_bytes,
            the_chunk_index,
            &last_written_bytes,
            first_chunk_offset,
            just_sending_flag
            );

        if (err_code)
            return err_code;

        _SC_ASSERT(0 != last_written_bytes);

        // Increasing total sent bytes.
        total_processed_bytes += last_written_bytes;
        _SC_ASSERT(total_processed_bytes <= buf_len_bytes);

        uint8_t* cur_chunk_buf;
        uint32_t err_code = cm_get_shared_memory_chunk(the_chunk_index, &cur_chunk_buf);
        _SC_ASSERT(err_code == 0);

        shared_memory_chunk* smc = (shared_memory_chunk*)cur_chunk_buf;
        if (last_written_bytes <= starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - first_chunk_offset) {
            _SC_ASSERT(smc->is_terminated());
        }

        // Checking if we have processed everything.
        if (total_processed_bytes < buf_len_bytes)
        {
            // Obtaining a new chunk and copying the original chunk header there.
            starcounter::core::chunk_index new_chunk_index;
            err_code = sc_bmx_clone_chunk(the_chunk_index, 0, starcounter::MixedCodeConstants::CHUNK_NUM_CLONE_BYTES, &new_chunk_index);
            if (err_code) {
                return err_code;
            }

            // Sending linked chunks to client.
            err_code = cm_send_to_client(gw_worker_id, the_chunk_index);
            _SC_ASSERT(err_code == 0);

            // Switching to next cloned chunk.
            the_chunk_index = new_chunk_index;

            // The rest of the data is just for sending.
            just_sending_flag = true;

            //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;
        }
        else
        {
            // Sending linked chunks to client.
            err_code = cm_send_to_client(gw_worker_id, the_chunk_index);
            _SC_ASSERT(err_code == 0);
            the_chunk_index = shared_memory_chunk::link_terminator;

            //std::cout << "Sent bytes: " << total_sent_bytes << std::endl;

            break;
        }
    }

    return 0;
}

// The entry point to send any-size user data to client.
EXTERN_C uint32_t __stdcall sc_bmx_send_buffer(
    const uint8_t gw_worker_id,
    uint8_t* const buf,
    const int32_t buf_len_bytes,
    starcounter::core::chunk_index* the_chunk_index,
    const uint32_t conn_flags
    )
{
    _SC_ASSERT(shared_memory_chunk::link_terminator != *the_chunk_index);
    _SC_ASSERT(buf_len_bytes >= 0);

    ProfilerStart(starcounter::utils::ProfilerEnums::Empty);
    ProfilerStop(starcounter::utils::ProfilerEnums::Empty);

    uint32_t err_code;

    uint8_t* cur_chunk_buf;
    err_code = cm_get_shared_memory_chunk(*the_chunk_index, &cur_chunk_buf);
    _SC_ASSERT(err_code == 0);
    _SC_ASSERT(NULL != cur_chunk_buf);

    // Setting total data length.
    *(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_USER_DATA_TOTAL_LENGTH_FROM_DB) = buf_len_bytes;

    // Points to user data offset in chunk.
    uint32_t chunk_user_data_offset = starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_DATA + starcounter::MixedCodeConstants::SOCKET_DATA_OFFSET_BLOB;

    // Adding connection flags.
    (*(uint32_t*)(cur_chunk_buf + starcounter::MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) |= conn_flags;

    int32_t remaining_bytes_in_orig_chunk = (starcounter::MixedCodeConstants::CHUNK_MAX_DATA_BYTES - chunk_user_data_offset);

    // Setting non-bmx-management chunk type.
    (*(BMX_HANDLER_TYPE*)(cur_chunk_buf + starcounter::core::chunk_type::bmx_protocol_begin)) = starcounter::bmx::BMX_INVALID_HANDLER_INFO;

    // Setting request size to zero.
    (*(uint32_t*)(cur_chunk_buf + starcounter::core::chunk_type::request_size_begin)) = 0;

    // Checking if user data fits inside the request chunk.
    if (buf_len_bytes <= remaining_bytes_in_orig_chunk)
    {
        // Sending using the same request chunk.
        err_code = sc_bmx_send_small_buffer(gw_worker_id, buf, buf_len_bytes, *the_chunk_index, chunk_user_data_offset);
        _SC_ASSERT(err_code == 0);
    }
    else
    {
        // Sending using multiple linked chunks.
        err_code = sc_bmx_send_big_buffer(gw_worker_id, buf, buf_len_bytes, *the_chunk_index, chunk_user_data_offset);
    }

    // Returning chunk if any error etc.
    if (shared_memory_chunk::link_terminator != *the_chunk_index) {

        _SC_ASSERT(0 != err_code);
        cm_release_linked_shared_memory_chunks(*the_chunk_index);
        *the_chunk_index = shared_memory_chunk::link_terminator;
    }

    return err_code;
}

// Releases given linked chunks.
EXTERN_C void __stdcall sc_bmx_release_linked_chunks(starcounter::core::chunk_index* the_chunk_index)
{
    uint32_t err_code = cm_release_linked_shared_memory_chunks(*the_chunk_index);
    _SC_ASSERT(0 == err_code);

    *the_chunk_index = shared_memory_chunk::link_terminator;
}

}  // namespace bmx
}; // namespace starcounter
