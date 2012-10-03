//
// impl/database_shared_memory_parameters.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class instance_shared_memory_parameters.
//

#ifndef STARCOUNTER_CORE_IMPL_DATABASE_SHARED_MEMORY_PARAMETERS_HPP
#define STARCOUNTER_CORE_IMPL_DATABASE_SHARED_MEMORY_PARAMETERS_HPP

// Implementation

namespace starcounter {
namespace core {

inline database_shared_memory_parameters::database_shared_memory_parameters()
: sequence_number_(0) {}

inline database_shared_memory_parameters::sequence_number_type
database_shared_memory_parameters::get_sequence_number() {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_);
	
	return sequence_number_;
}

inline const database_shared_memory_parameters&
database_shared_memory_parameters::set_sequence_number
(sequence_number_type sequence_number) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_);
	
	sequence_number_ = sequence_number;
	return *this;
}

inline const database_shared_memory_parameters&
database_shared_memory_parameters::increment_sequence_number() {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_);
	
	++sequence_number_;
	return *this;
}

inline const char* database_shared_memory_parameters::get_server_name() const {
	return static_cast<const char*>(server_name_);
}

inline void database_shared_memory_parameters::set_server_name(const char* name)
{
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_);
	
	strncpy(server_name_, name, server_name_size -1);
	server_name_[server_name_size -1] = 0;
}

inline void database_shared_memory_parameters::set_server_name(const wchar_t*
name) {
	char temp_buffer[server_name_size];
	
	// Convert wide-character string to multibyte string.
	std::size_t server_name_length = std::wcstombs(temp_buffer, name,
	server_name_size -1);
	
	temp_buffer[server_name_length] = 0;
	
	{
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_);
		
		strncpy(server_name_, temp_buffer, server_name_size -1);
		server_name_[server_name_size -1] = 0;
	}
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_DATABASE_SHARED_MEMORY_PARAMETERS_HPP
