#pragma once
#ifndef HTTP_REQUEST_HPP
#define HTTP_REQUEST_HPP

#include "http_common.hpp"

namespace starcounter {
namespace network {

struct HttpRequest
{
    uint32_t request_len_bytes_;
    uint32_t content_len_bytes_;

    uint16_t request_offset_;
    uint16_t content_offset_;
    uint16_t uri_len_bytes_;
    uint16_t headers_offset_;
    uint16_t headers_len_bytes_;
    uint16_t session_string_offset_;
    uint16_t uri_offset_;

    uint8_t http_method_;
    uint8_t gzip_accepted_;

    // TODO: Should be removed!
    uint64_t socket_data_padding_;

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
