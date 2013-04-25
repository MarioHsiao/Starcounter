#pragma once
#ifndef HTTP_REQUEST_HPP
#define HTTP_REQUEST_HPP

#include "http_common.hpp"

namespace starcounter {
namespace network {

struct HttpRequest
{
    // Request.
    uint32_t request_offset_;
    uint32_t request_len_bytes_;

    // Content.
    uint32_t content_offset_;
    uint32_t content_len_bytes_;

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
    uint32_t header_offsets_[MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS];
    uint32_t header_len_bytes_[MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS];
    uint32_t header_value_offsets_[MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS];
    uint32_t header_value_len_bytes_[MixedCodeConstants::MAX_PREPARSED_HTTP_REQUEST_HEADERS];
    uint32_t num_headers_;

    // HTTP method.
    bmx::HTTP_METHODS http_method_;

    // Is Gzip accepted.
    bool gzip_accepted_;

    // Resets this instance of request.
    void Reset()
    {
        memset(this, 0, sizeof(HttpRequest));
    }
};

// Initializing HTTP request parser data structures.
uint32_t sc_init_http_request_parser();

} // namespace network
} // namespace starcounter

#endif // HTTP_REQUEST_HPP
