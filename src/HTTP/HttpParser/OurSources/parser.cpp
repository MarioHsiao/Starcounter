#include "http_parser.h"
#include "http_request.hpp"

namespace starcounter {
namespace network {

inline int OnMessageBegin(http_parser* p)
{

    return 0;
}

inline int OnHeadersComplete(http_parser* p)
{


    return 0;
}

inline int OnMessageComplete(http_parser* p)
{

    return 0;
}

inline int OnUri(http_parser* p, const char *at, size_t length)
{
    return 0;
}

inline int OnHeaderField(http_parser* p, const char *at, size_t length)
{

    return 0;
}

inline int OnHeaderValue(http_parser* p, const char *at, size_t length)
{
    return 0;
}

inline int OnBody(http_parser* p, const char *at, size_t length)
{

    return 0;
}

// Global HTTP parser settings.
http_parser_settings g_httpParserSettings;

void HttpGlobalInit()
{
    // Setting HTTP callbacks.
    g_httpParserSettings.on_body = OnBody;
    g_httpParserSettings.on_header_field = OnHeaderField;
    g_httpParserSettings.on_header_value = OnHeaderValue;
    g_httpParserSettings.on_headers_complete = OnHeadersComplete;
    g_httpParserSettings.on_message_begin = OnMessageBegin;
    g_httpParserSettings.on_message_complete = OnMessageComplete;
    g_httpParserSettings.on_url = OnUri;
}



// Parses HTTP request from the given buffer and returns corresponding instance of HttpRequest.
uint32_t sc_parse_http_request(uint8_t* request_bytes, uint32_t length_bytes, HttpRequest* out_request)
{
    return 0;
}

} // namespace network
} // namespace starcounter