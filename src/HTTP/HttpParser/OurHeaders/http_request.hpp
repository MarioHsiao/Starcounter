#pragma once
#ifndef HTTP_REQUEST_HPP
#define HTTP_REQUEST_HPP

#include "../../../Chunks/bmx/bmx.hpp"

namespace starcounter {
namespace network {

// Maximum number of pre-parsed HTTP headers.
const int32_t MAX_HTTP_HEADERS = 16;

struct HttpRequest
{
    // Request.
    uint32_t request_offset_;
    uint32_t request_len_bytes_;

    // Body.
    uint32_t body_offset_;
    uint32_t body_len_bytes_;

    // Resource URI.
    uint32_t uri_offset_;
    uint32_t uri_len_bytes_;

    // Key-value header.
    uint32_t headers_offset_;
    uint32_t headers_len_bytes_;

    // Cookie value.
    uint32_t cookies_offset_;
    uint32_t cookies_len_bytes_;

    // Accept value.
    uint32_t accept_value_offset_;
    uint32_t accept_value_len_bytes_;

    // Session ID.
    uint32_t session_string_offset_;
    uint32_t session_string_len_bytes_;

    // Header offsets.
    uint32_t header_offsets_[MAX_HTTP_HEADERS];
    uint32_t header_len_bytes_[MAX_HTTP_HEADERS];
    uint32_t header_value_offsets_[MAX_HTTP_HEADERS];
    uint32_t header_value_len_bytes_[MAX_HTTP_HEADERS];
    uint32_t num_headers_;

    // HTTP method.
    bmx::HTTP_METHODS http_method_;

    // Is Gzip accepted.
    bool gzip_accepted_;
};

} // namespace network
} // namespace starcounter

#endif // HTTP_REQUEST_HPP
