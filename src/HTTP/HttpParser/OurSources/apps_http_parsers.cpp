#include "http_parser.h"
#include "http_request.hpp"
#include "http_response.hpp"

namespace starcounter {
namespace network {

// Initializes the internal Apps HTTP request parser.
EXTERN_C uint32_t sc_init_http_parser()
{
    uint32_t err_code = sc_init_http_request_parser();
    if (err_code)
        return err_code;

    err_code = sc_init_http_response_parser();
    if (err_code)
        return err_code;

    return 0;
}

} // namespace network
} // namespace starcounter