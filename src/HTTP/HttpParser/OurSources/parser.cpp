#include "http_parser.h"
#include "http_request.hpp"

namespace starcounter {
namespace network {

// Parses HTTP request from the given buffer and returns corresponding instance of HttpRequest.
uint32_t sc_parse_http_request(uint8_t* request_bytes, uint32_t length_bytes, HttpRequest* out_request)
{
    return 0;
}

} // namespace network
} // namespace starcounter