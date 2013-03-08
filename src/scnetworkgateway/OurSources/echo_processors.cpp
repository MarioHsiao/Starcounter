#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"

namespace starcounter {
namespace network {

#ifdef GW_TESTING_MODE

#ifdef GW_LOOPED_TEST_MODE

uint32_t DefaultHttpEchoRequestCreator(char* buf, echo_id_type echo_id, uint32_t* num_request_bytes)
{
    // Generating HTTP request.
    uint32_t http_request_len = g_gateway.GenerateHttpRequest(buf, echo_id);

    // Setting data length.
    *num_request_bytes = http_request_len;

    return 0;
}

uint32_t DefaultHttpEchoResponseProcessor(char* buf, uint32_t buf_len, echo_id_type* echo_id)
{
    // Asserting correct number of bytes received.
    GW_ASSERT(buf_len == kHttpEchoResponseLength);

    // Obtaining original echo number.
    *echo_id = hex_string_to_uint64(buf + kHttpEchoResponseInsertPoint, kHttpEchoContentLength);

    return 0;
}

uint32_t DefaultRawEchoRequestCreator(char* buf, echo_id_type echo_id, uint32_t* num_request_bytes)
{
    // Duplicating this echo.
    *(int64_t*)buf = echo_id;

    // Setting data length.
    *num_request_bytes = 8;

    return 0;
}

uint32_t DefaultRawEchoResponseProcessor(char* buf, uint32_t buf_len, echo_id_type* echo_id)
{
    // Asserting correct number of bytes received.
    GW_ASSERT(buf_len == 16);

    // Obtaining original echo number.
    *echo_id = *(int32_t*)(buf + 8);

    return 0;
}

#endif
#endif

} // namespace network
} // namespace starcounter
