
#ifndef STARCOUNTER_BLAST2_H
#define STARCOUNTER_BLAST2_H

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <stdint.h>
#include "sccoredata.h"
#include "../../Starcounter.ErrorCodes/scerrres/scerrres.h"
#include "../common/config_param.hpp"
#include "../common/chunk.hpp"
#include "../common/macro_definitions.hpp"

class chunk_part
{
private:
	starcounter::core::chunk_type::message_size_type offset_;
	uint8_t chunk_[1];

	template<typename T>
	void write(T value)
	{
		*((PACKED T*)(chunk_ + offset_)) = value;
		offset_ += sizeof(T);
	}

	template<typename T>
	T read()
	{
		T value = *((PACKED T*)(chunk_ + offset_));
		offset_ += sizeof(T);
		return value;
	}

	template<typename T>
	T peek()
	{
		return *((PACKED T*)(chunk_ + offset_));
	}


public:

	starcounter::core::chunk_type::message_size_type get_offset()
	{
		return offset_;
	}

	void reset_offset()
	{
		offset_ = 0;
	}

	void write_type_id(uint8_t type_id)
	{
		chunk_[offset_] = type_id << 1;
		offset_++;
	}

	uint8_t read_type_id()
	{
		uint8_t type_id = (chunk_[offset_] >> 1);
		offset_++;
		return type_id;
	}

    BMX_HANDLER_TYPE read_handler_id()
    {
        return read_int16();
    }

    void write_handler_id(BMX_HANDLER_TYPE handler_id)
    {
        write(handler_id);
    }

	void skip(uint32_t n) { offset_ += n; }
	uint8_t* get_raw_chunk() { return chunk_ + offset_; }

	int8_t read_int8() { return read<int8_t>(); }
	uint8_t read_uint8() { return read<uint8_t>(); }
	int16_t read_int16() { return read<int16_t>(); }
	uint16_t read_uint16() { return read<uint16_t>(); }
	int32_t read_int32() { return read<int32_t>(); }
	uint32_t read_uint32() { return read<uint32_t>(); }
	int64_t read_int64() { return read<int64_t>(); }
	uint64_t read_uint64() { return read<uint64_t>(); }
	float read_float() { return read<float>(); }
	double read_double() { return read<double>(); }

    uint32_t read_wstring(wchar_t* dest_str, uint32_t num_chars_to_copy, uint32_t max_len_chars)
    {
        // Copying the URI string.
        if (wcsncpy_s(dest_str, max_len_chars, (wchar_t *)get_raw_chunk(), num_chars_to_copy))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Skipping all read characters.
        skip(num_chars_to_copy << 1);

        return 0;
    }

    uint32_t read_string(char* dest_str, uint32_t num_chars_to_copy, uint32_t max_len_chars)
    {
        // Copying the URI string.
        if (strncpy_s(dest_str, max_len_chars, (char *)get_raw_chunk(), num_chars_to_copy))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Skipping all read characters.
        skip(num_chars_to_copy);

        return 0;
    }

	int32_t peek_int32() { return peek<int32_t>(); }
	uint32_t peek_uint32() { return peek<uint32_t>(); }

	double read_compressed_double()
	{
		uint8_t* p = chunk_ + offset_;
		int32_t data_size = _DoubleSize2_Bytes(p);
		offset_ += data_size;
		return _DoubleRead2(p);
	}

	float read_compressed_float()
	{
		uint8_t* p = chunk_ + offset_;
		int32_t data_size = _SingleSize2_Bytes(p);
		offset_ += data_size;
		return _SingleRead2(p);
	}

	uint64_t read_compressed_unsigned_int()
	{
		uint8_t* p = chunk_ + offset_;
		int32_t size = Mdb_UInt4To64Size_Bytes(p);
		offset_ += size;
		return Mdb_UInt4To64Read(p);
	}

	int64_t read_compressed_signed_int()
	{
		uint8_t* p = chunk_ + offset_;
		int32_t size = Mdb_Int4To64Size_Bytes(p);
		offset_ += size;
		return Mdb_Int4To64Read(p);
	}

	void write(int8_t value) { write<int8_t>(value); }
	void write(uint8_t value) { write<uint8_t>(value); }
	void write(int16_t value) { write<int16_t>(value); }
	void write(uint16_t value) { write<uint16_t>(value); }
	void write(int32_t value) { write<int32_t>(value); }
	void write(uint32_t value) { write<uint32_t>(value); }
	void write(int64_t value) { write<int64_t>(value); }
	void write(uint64_t value) { write<uint64_t>(value); }
	void write(float value) { write<float>(value); }
	void write(double value) { write<double>(value); }

    uint32_t write_wstring(wchar_t* str, uint32_t num_chars_to_write)
    {
        // Writing number of characters in the string.
        write(num_chars_to_write);

        // Checking if enough space.
        if (num_chars_to_write >= ((starcounter::core::chunk_size - offset_) >> 1))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Copying the URI string.
        if (wcsncpy_s((wchar_t *)get_raw_chunk(), num_chars_to_write + 1, str, num_chars_to_write))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Skipping all written characters.
        skip(num_chars_to_write << 1);

        return 0;
    }

    uint32_t write_string(char* str, uint32_t num_chars_to_write)
    {
        // Writing number of characters in the string.
        write(num_chars_to_write);

        // Checking if enough space.
        if (num_chars_to_write >= (starcounter::core::chunk_size - offset_))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Copying the URI string.
        if (strncpy_s((char *)get_raw_chunk(), num_chars_to_write + 1, str, num_chars_to_write))
            return SCERRUNSPECIFIED; // SCERRURISTRINGCOPYPROBLEM

        // Skipping all written characters.
        skip(num_chars_to_write);

        return 0;
    }

	void write_compressed_unsigned_int(uint64_t value)
	{
		uint8_t* p = chunk_ + offset_;
		int32_t size = Mdb_UInt64Size_UInt4To64(value);
		Mdb_UInt4To64Write_Balanced(p, value);
		offset_ += size;
	}

	void write_compressed_signed_int(int64_t value)
	{
		uint8_t* p = chunk_ + offset_;
		int32_t size = Mdb_Int64Size_Int4To64(value);
		Mdb_Int4To64Write_Balanced(p, value);
		offset_ += size;
	}

	uint32_t write_data_and_size(uint8_t* data, uint32_t data_size)
	{
		// TODO: 
		// Check for buffer overrun
		uint8_t* p = chunk_ + offset_;

		*((PACKED int32_t*)p) = data_size;
		memcpy(p + 4, data, data_size);
		offset_ += data_size + 4;
		return 0;
	}

	uint32_t write_data_only(uint8_t* data, uint32_t data_size)
	{
		// TODO: 
		// Check for buffer overrun
		uint8_t* p = chunk_ + offset_;
		memcpy(p, data, data_size);
		offset_ += data_size;
		return 0;
	}

	uint32_t copy_data_to_buffer(uint8_t* buffer, uint32_t size)
	{
		uint8_t* p;

		p = chunk_ + offset_;
		memcpy(buffer, p, size);
		offset_ += size;
		return 0;
	}
};

typedef chunk_part request_chunk_part ;
typedef chunk_part response_chunk_part;

template<class T, std::size_t N>
class blast_chunk : public starcounter::core::chunk<T, N>
{
public:
	request_chunk_part* get_request_chunk() 
	{ 
		return (chunk_part*)(elems + REQUEST_SIZE_BEGIN);
	}

	response_chunk_part* get_response_chunk()
	{
		uint32_t offset = STATIC_HEADER_SIZE + get_request_size();
		return (chunk_part*)(elems + offset);
	}

	uint32_t get_available_request_size()
	{
		return ((uint32_t)size()
				- STATIC_HEADER_SIZE 
				- get_request_size()
				- sizeof (link_type)
		);
	}

	uint32_t get_available_response_size()
	{
		return ((uint32_t)size()
				- STATIC_HEADER_SIZE // including size of request size type
				- get_request_size()
				- get_response_chunk()->get_offset()
				- sizeof (message_size_type) // size of response size type
				- sizeof (link_type)
		);
	}

	// TODO:
	// Change this call. We don't want to have several places where the header
	// is written in case of changes to it.
	uint8_t* get_raw_chunk()
	{
		return (uint8_t*)elems;
	}
};

// TODO:
// rename shared_memory_chunk to something better.
typedef blast_chunk<uint8_t, starcounter::core::chunk_size> shared_memory_chunk;

INLINE uint32_t on_dummy(response_chunk_part* response_chunk, shared_memory_chunk* main_chunk)
{
	return 1;
}

#endif
