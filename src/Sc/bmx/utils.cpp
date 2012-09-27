
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