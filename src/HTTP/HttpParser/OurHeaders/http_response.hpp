#pragma once
#ifndef HTTP_RESPONSE_HPP
#define HTTP_RESPONSE_HPP

#include "http_common.hpp"

namespace starcounter {
namespace network {

struct HttpResponse
{
    uint32_t response_len_bytes_;
    int32_t content_len_bytes_;

    uint16_t response_offset_;
    uint16_t content_offset_;
    uint16_t headers_offset_;
    uint16_t headers_len_bytes_;
    uint16_t session_string_offset_;
    uint16_t status_code_;

    uint8_t session_string_len_bytes_;

    // TODO: Should be removed!
    uint64_t socket_data_padding_;

    // Resets this instance of request.
    void Reset()
    {
        memset(this, 0, sizeof(HttpResponse));
    }
};

// Initializing HTTP response parser data structures.
uint32_t sc_init_http_response_parser();

} // namespace network
} // namespace starcounter

#endif // HTTP_RESPONSE_HPP
