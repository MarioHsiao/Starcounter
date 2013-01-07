//
// shared_memory_segment_remover.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SHARED_MEMORY_SEGMENT_REMOVER_HPP
#define STARCOUNTER_CORE_SHARED_MEMORY_SEGMENT_REMOVER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <memory>
#include <string>
#include <algorithm>
#include <utility>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "../common/client_number.hpp"
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/channel_number.hpp"
#include "../common/client_number.hpp"
#include "../common/common_scheduler_interface.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/common_client_interface.hpp"
#include "../common/client_interface.hpp"
#include "../common/scheduler_channel.hpp"
#include "../common/scheduler_number.hpp"
#include "../common/owner_id.hpp"
#include "../common/pid_type.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"

#if defined(_WIN32) || defined(_WIN64)
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <Tchar.h>
#undef WIN32_LEAN_AND_MEAN
#endif // defined(_WIN32) || defined(_WIN64)

namespace starcounter {
namespace core {

/// ------------------------------------------------------------------------
/// class shared_memory_segment_remover.
/// It finds all shared memory segments under the given path (which is hard-
/// coded to "C:\\Users\\All Users\\boost_interprocess" in the public beta),
/// beginning with a prefix_name and removes them. The prefix_name is
/// "starcounter".

#if 0 // obsolete
class shared_memory_segment_remover {
public:
	// exception class
	class bad_shared_memory_segment_remover {};

	#if defined(_WIN32) || defined(_WIN64)
	typedef TCHAR char_type; // char or wchar_t depending on macro _UNICODE
	typedef std::basic_string<char_type> string_type;
	#endif // defined(_WIN32) || defined(_WIN64)

	// ---------------------------------------------------------------------
	// constructor
	shared_memory_segment_remover(std::string path
	= SHARED_MEMORY_SEGMENTS_PATH, std::string prefix
	= DATABASE_NAME_PREFIX) {
		std::string file_name = path +prefix +"*.*"; // or "*.*"?
		std::cout << "file_name: " << file_name << std::endl; // DEBUG
		bool found = false;

		// Convert file_name to a string_type suitable for FindFirstFile().
		buffer_.resize(file_name.length());
		std::copy(file_name.begin(), file_name.end(), buffer_.begin());
		buffer_.push_back(static_cast<char_type>('\0')); // Append null.

		// -----------------------------------------------------------------
		// Find the first file, if any.
		if ((found_ = FindFirstFile(&buffer_[0], &find_data_))
		== INVALID_HANDLE_VALUE) {
			std::cout << "INVALID_HANDLE_VALUE" << std::endl;
			// This indicates failure. Get extended error information.
			DWORD err = GetLastError(); // But what to do with it?
			std::cout << "error: " << err << " now throwing..." << std::endl;
			// 2 = The system cannot find the file specified.
			throw bad_shared_memory_segment_remover();
		}
		else {
			found = true;
		}

		// DeleteFile() wants char_type formated file names. 
		std::vector<char_type> file_to_delete(MAX_PATH);

		// Copy the path first
		std::copy(path.begin(), path.end(), file_to_delete.begin());
		
		//file_to_delete[path.length()] = static_cast<char_type>('\0');
		
		std::cout << "path = ";

		for (std::size_t i = 0; i < file_to_delete.size(); ++i) {
			std::cout << (char) file_to_delete[i];
		}
		std::cout << std::endl;

		while (found) {
			// -------------------------------------------------------------

			// Copy the file name
			for (std::size_t i = 0; i < MAX_PATH -file_to_delete.size();
			++i) {
				char c = find_data_.cFileName[i];
				file_to_delete.push_back(static_cast<char_type>(c));

				/// DEBUG
				if (c) {
					std::cout << (char) find_data_.cFileName[i];
				}
				else {
					break;
				}
			}
			file_to_delete.push_back(0); /// NEEDED?
			
			//buffer_.push_back(static_cast<char_type>('\0')); // Append null.

			if (DeleteFile(&file_to_delete[0]) == 0) {
				std::cout << "deleting the file failed" << std::endl;
				switch (GetLastError()) {
				case ERROR_FILE_NOT_FOUND:
					// Attempting to delete a file that does not exist
					std::cout << "error: file does not exist" << std::endl;
					break;
				case ERROR_ACCESS_DENIED:
					std::cout << "error: read-only file" << std::endl;
					break;
				default:
					std::cout << "error: unknown" << std::endl;
				}
				/// The following list identifies some tips for deleting,
				/// removing, or closing files:
				/// To delete a read-only file, first you must remove the
				/// read-only attribute.
				/// To delete or rename a file, you must have either delete
				/// permission on the file, or delete child permission in
				/// the parent directory.
				/// To recursively delete the files in a directory, use the
				/// SHFileOperation function.
				/// To remove an empty directory, use the RemoveDirectory
				/// function.
				/// To close an open file, use the CloseHandle function.
			}

			// Find the next file, if any.
			if (FindNextFile(found_, &find_data_) == 0) {
				// Zero indicates failure.
				if (GetLastError() == ERROR_NO_MORE_FILES) {
					std::cout << "--- no more files ---" << std::endl;
					// No matching files can be found.
					found = false;
				}
				else {
					// Unknown error.
					throw bad_shared_memory_segment_remover();
				}
			}
		}
		// Done.
	}

	// destructor
	~shared_memory_segment_remover() {
		if (FindClose(found_) == 0) {
			// Zero indicates failure. Get extended error information.
			DWORD err = GetLastError(); // But what to do with it?
		}
	}

private:
	WIN32_FIND_DATA find_data_;
	HANDLE found_;
	std::vector<char_type> buffer_;

	// I don't think these are needed.
	std::string segment_name_;
	boost::interprocess::managed_shared_memory segment_;
};

#endif // obsolete

} // namespace core
} // namespace starcounter

//#include "impl/shared_memory_segment_remover.hpp"

#endif // STARCOUNTER_CORE_SHARED_MEMORY_SEGMENT_REMOVER_HPP
