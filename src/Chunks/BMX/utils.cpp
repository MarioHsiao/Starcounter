
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
	request_chunk_part* request_chunk;
	shared_memory_chunk* smc;
	uint32_t request_size;
	uint32_t link_index;
	
	smc = (shared_memory_chunk*)raw_chunk;
	request_chunk = smc->get_request_chunk();
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

EXTERN_C uint32_t __stdcall sc_bmx_clone_chunk(
    starcounter::core::chunk_index src_chunk_index,
    uint32_t offset,
    uint32_t num_bytes_to_copy,
    starcounter::core::chunk_index* new_chunk_index
    )
{
    uint32_t err_code;

    // Acquiring new chunk.
    err_code = cm_acquire_shared_memory_chunk((DWORD *)new_chunk_index);
    if (err_code) return err_code;

    // Getting chunk memory address.
    uint8_t* src_chunk_buf;
    err_code = cm_get_shared_memory_chunk(src_chunk_index, &src_chunk_buf);
    if (err_code) return err_code;

    // Getting chunk memory address.
    uint8_t* new_chunk_buf;
    err_code = cm_get_shared_memory_chunk(*new_chunk_index, &new_chunk_buf);
    if (err_code) return err_code;

    // Copying memory from original chunk to copy.
    memcpy(new_chunk_buf, src_chunk_buf + offset, num_bytes_to_copy);

    return 0;
}

#define MAX_LINKED_CHUNKS_BYTES 1024 * 32

EXTERN_C uint32_t __stdcall sc_bmx_write_to_chunks(
    uint8_t* buf,
    uint32_t buf_len_bytes,
    starcounter::core::chunk_index chunk_index,
    uint32_t* actual_written_bytes
    )
{
    uint32_t num_write_bytes_total = buf_len_bytes;
    starcounter::core::chunk_index cur_chunk_index = chunk_index;
    
    // Checking if more than maximum chunks we can take at once.
    if (buf_len_bytes > MAX_LINKED_CHUNKS_BYTES)
        num_write_bytes_total = MAX_LINKED_CHUNKS_BYTES;

    // Acquiring linked chunks.
    uint32_t err_code = SCERRUNSPECIFIED;
    while (err_code)
    {
        err_code = cm_acquire_linked_shared_memory_chunks(chunk_index, num_write_bytes_total);
        std::cout << "Can't acquire linked chunks! Sleeping 1 second." << std::endl;
        Sleep(1000);
    }

    // Getting chunk memory address.
    uint8_t* cur_chunk_buf;
    err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
    if (err_code) return err_code;

    // Getting index of the following chunk.
    cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();

    // Going through each linked chunk and write data there.
    uint32_t left_bytes = num_write_bytes_total;
    uint32_t num_bytes_to_write_in_chunk = starcounter::core::chunk_size - shared_memory_chunk::LINK_SIZE;
    while(left_bytes > 0)
    {
        // Getting chunk memory address.
        err_code = cm_get_shared_memory_chunk(cur_chunk_index, &cur_chunk_buf);
        if (err_code) return err_code;
        
        // Copying memory.
        memcpy(cur_chunk_buf, buf + num_write_bytes_total - left_bytes, num_bytes_to_write_in_chunk);
        left_bytes -= num_bytes_to_write_in_chunk;

        // Checking how many bytes to write next time.
        if (left_bytes < (starcounter::core::chunk_size - shared_memory_chunk::LINK_SIZE))
            num_bytes_to_write_in_chunk = left_bytes;

        // Getting next chunk in chain.
        cur_chunk_index = ((shared_memory_chunk*)cur_chunk_buf)->get_link();
    }

    // Setting number of total written bytes.
    *actual_written_bytes = num_write_bytes_total;

    return 0;
}

EXTERN_C uint32_t __stdcall sc_bmx_send_big_buffer(
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
        // Copying user data to multiple chunks.
        err_code = sc_bmx_write_to_chunks(
            buf + total_sent_bytes,
            buf_len_bytes - total_sent_bytes,
            src_chunk_index,
            &last_written_bytes
            );
        if (err_code) return err_code;

        // Increasing total sent bytes.
        total_sent_bytes += last_written_bytes;

        // Checking if not all user data has been copied so we need to make a chunk clone.
        if (total_sent_bytes < buf_len_bytes)
        {
            // Obtaining a new chunk and copying the original chunk header there.
            err_code = sc_bmx_clone_chunk(src_chunk_index, 0, 512, &new_chunk_index);
            if (err_code) return err_code;
        }

        // Sending linked chunks to client.
        err_code = cm_send_to_client(src_chunk_index);
        if (err_code) return err_code;

        // Assigning new source chunk.
        src_chunk_index = new_chunk_index;
    }

    return 0;
}

/*
EXTERN_C uint32_t __stdcall sc_bmx_write_to_chunk(
	uint8_t* source_buffer,
	uint32_t length,
	uint32_t* chunk_index,
    uint32_t offset
)
{
	shared_memory_chunk* current_smc;
	shared_memory_chunk* main_chunk;
	response_chunk_part* response;
	request_chunk_part* current_part;
	uint8_t* p;
	uint8_t* temp_chunk;
	uint32_t available_response_size;
	uint32_t copy_size;
	uint32_t errorcode;
	uint32_t left_to_copy;
	uint32_t needed_size;
	uint32_t next_index;

	DWORD new_chunk_index;
//	errorcode = cm_acquire_shared_memory_chunk(&new_chunk_index);
//	if (errorcode != 0) goto on_error;
	new_chunk_index = *chunk_index;

	errorcode = cm_get_shared_memory_chunk(new_chunk_index, &temp_chunk);
	if (errorcode != 0) goto on_error;

	main_chunk = (shared_memory_chunk*)temp_chunk;
	response = (response_chunk_part *)(((char *)main_chunk->get_response_chunk()) - shared_memory_chunk::LINK_SIZE);
	response->reset_offset();
	available_response_size = main_chunk->get_available_response_size();

	if (available_response_size > length) 
	{
        response->write(255);
        response->skip(offset);
		response->write_data_only(source_buffer, length);
		*chunk_index = new_chunk_index;
		return 0;
	}

	// More than one chunk is needed.
	needed_size = length - available_response_size;
	errorcode = cm_acquire_linked_shared_memory_chunks(new_chunk_index, needed_size);
	if (errorcode != 0) goto on_error;

	response->write((int32_t)length);

	p = source_buffer;
	response->write_data_only(p, available_response_size);
	p += available_response_size;
	left_to_copy = needed_size;

	next_index = main_chunk->get_link();
	while (next_index != main_chunk->LINK_TERMINATOR)
	{
		errorcode = cm_get_shared_memory_chunk(next_index, &temp_chunk);
		if (errorcode != 0) goto on_error;

		current_smc = (shared_memory_chunk*)temp_chunk;

		// We use the request part of the chunk here to gain 4 bytes since we are going to
		// use the whole chunk (probably).
		current_part = current_smc->get_request_chunk();
		current_part->reset_offset();

		copy_size = current_smc->get_available_request_size();
		if (left_to_copy < copy_size) copy_size = left_to_copy;

		current_part->write_data_only(p, copy_size);
		p += copy_size;
		left_to_copy -= copy_size;
		next_index = current_smc->get_link();
	}

	*chunk_index = new_chunk_index;
	return 0;

on_error:
	// TODO:
	// if we have allocated the chunks already we need to return them here
	// before returning.
	return errorcode;
}
*/