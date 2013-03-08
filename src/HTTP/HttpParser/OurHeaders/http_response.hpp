#pragma once
#ifndef HTTP_RESPONSE_HPP
#define HTTP_RESPONSE_HPP

#include "http_common.hpp"

namespace starcounter {
namespace network {

struct HttpResponse
{
    // Response offset.
    uint32_t response_offset_;
    uint32_t response_len_bytes_;

    // Content.
    uint32_t content_offset_;
    uint32_t content_len_bytes_;

    // Key-value header.
    uint32_t headers_offset_;
    uint32_t headers_len_bytes_;

    // Cookie value.
    uint32_t set_cookies_offset_;
    uint32_t set_cookies_len_bytes_;

    // Session ID.
    uint32_t session_string_offset_;
    uint32_t session_string_len_bytes_;

    // Header offsets.
    uint32_t header_offsets_[MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
    uint32_t header_len_bytes_[MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
    uint32_t header_value_offsets_[MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
    uint32_t header_value_len_bytes_[MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS];
    uint32_t num_headers_;
};

// Initializing HTTP response parser data structures.
uint32_t sc_init_http_response_parser();

} // namespace network
} // namespace starcounter

#endif // HTTP_RESPONSE_HPP
