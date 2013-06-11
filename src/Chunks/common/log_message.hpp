//
// log_message.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_LOG_MESSAGE_HPP
#define STARCOUNTER_CORE_LOG_MESSAGE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

namespace starcounter {
namespace core {

/// Class log_message.
class log_message {
public:
	explicit log_message(std::string s = std::string())
	: message_(s) {}
	
	const std::string& get_message() const {
		return message_;
	}
	
	void set_message(const std::string& s) {
		message_ = s;
	}
	
private:
	std::string message_;
};

} // namespace core
} // namespace starcounter

//#include "impl/log_message.hpp"

#endif // STARCOUNTER_CORE_LOG_MESSAGE_HPP
