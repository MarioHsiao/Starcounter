#include "http_parser.h"
#include "http_response.hpp"

namespace starcounter {
namespace network {

// Structure that is used during parsing.
struct HttpResponseParserStruct
{
    // An http_parser instance inside.
    http_parser http_parser_;

    // Structure that holds HTTP response.
    HttpResponse* http_response_;

    // HTTP/WebSockets fields.
    HttpWsFields last_field_;

    // Indicates if we have complete header.
    bool complete_header_flag_;

    // Pointer to response buffer.
    uint8_t* response_buf_;

    // Resets the parser related fields.
    void Reset(uint8_t* response_buf, HttpResponse* http_response)
    {
        response_buf_ = response_buf;
        http_response_ = http_response;

        last_field_ = UNKNOWN_FIELD;
        http_response_->Reset();

        http_parser_init(&http_parser_, HTTP_RESPONSE);
        complete_header_flag_ = false;
    }
};

// Global HTTP parser settings.
http_parser_settings* g_http_response_parser_settings = NULL;

// Declaring thread-safe parser structure.
__declspec(thread) HttpResponseParserStruct thread_http_response_parser;

inline int HttpResponseOnMessageBegin(http_parser* p)
{
    // Do nothing.
    return 0;
}

inline int HttpResponseOnHeadersComplete(http_parser* p)
{
    HttpResponseParserStruct *http = (HttpResponseParserStruct *)p;

    // Setting complete header flag.
    http->complete_header_flag_ = true;

    // Setting headers length (skipping 4 bytes for \r\n\r\n).
    http->http_response_->headers_len_bytes_ = p->nread - 4 - http->http_response_->headers_len_bytes_;

    return 0;
}

inline int HttpResponseOnMessageComplete(http_parser* p)
{
    // Do nothing.
    return 0;
}

inline int HttpResponseOnUri(http_parser* p, const char *at, size_t length)
{
    HttpResponseParserStruct *http = (HttpResponseParserStruct *)p;
    
    return 0;
}

inline int HttpResponseOnHeaderField(http_parser* p, const char *at, size_t length)
{
    HttpResponseParserStruct *http = (HttpResponseParserStruct *)p;

    // Determining what header field is that.
    http->last_field_ = DetermineField(at, length);

    // Saving header offset.
    http->http_response_->header_offsets_[http->http_response_->num_headers_] = (uint32_t)(at - (char*)http->response_buf_);
    http->http_response_->header_len_bytes_[http->http_response_->num_headers_] = (uint32_t)length;

    // Setting headers beginning.
    if (!http->http_response_->headers_offset_)
    {
        http->http_response_->headers_len_bytes_ = (uint32_t)(p->nread - length - 1);
        http->http_response_->headers_offset_ = (uint32_t)(at - (char*)http->response_buf_);
    }

    return 0;
}

inline int HttpResponseOnHeaderValue(http_parser* p, const char *at, size_t length)
{
    HttpResponseParserStruct *http = (HttpResponseParserStruct *)p;

    // Saving header length.
    http->http_response_->header_value_offsets_[http->http_response_->num_headers_] = (uint32_t)(at - (char*)http->response_buf_);
    http->http_response_->header_value_len_bytes_[http->http_response_->num_headers_] = (uint32_t)length;

    // Increasing number of saved headers.
    http->http_response_->num_headers_++;
    if (http->http_response_->num_headers_ >= MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS)
    {
        // Too many HTTP headers.
        std::cout << "Too many HTTP headers detected, maximum allowed: " << MixedCodeConstants::MAX_PREPARSED_HTTP_RESPONSE_HEADERS << std::endl;
        return 1;
    }

    // Processing last field type.
    switch (http->last_field_)
    {
        case SET_COOKIE_FIELD:
        {
            // Setting needed HttpResponse fields.
            http->http_response_->set_cookies_offset_ = (uint32_t)(at - (char*)http->response_buf_);
            http->http_response_->set_cookies_len_bytes_ = (uint32_t)length;

            break;
        }

        case CONTENT_LENGTH_FIELD:
        {
            // Calculating body length.
            http->http_response_->content_len_bytes_ = ParseDecimalStringToUint(at, length);

            break;
        }
    }

    return 0;
}

inline int HttpResponseOnBody(http_parser* p, const char *at, size_t length)
{
    HttpResponseParserStruct *http = (HttpResponseParserStruct *)p;

    // Setting body parameters.
    if (http->http_response_->content_len_bytes_ < 0)
        http->http_response_->content_len_bytes_ = (uint32_t)length;

    // Setting body data offset.
    http->http_response_->content_offset_ = (uint32_t)(at - (char*)http->response_buf_);

    return 0;
}

// Parses HTTP response from the given buffer and returns corresponding instance of HttpResponse.
EXTERN_C uint32_t __stdcall sc_parse_http_response(
    uint8_t* response_buf,
    uint32_t response_size_bytes,
    uint8_t* out_http_response)
{
    assert(g_http_response_parser_settings != NULL);

    // Resetting the parser structure.
    thread_http_response_parser.Reset(response_buf, (HttpResponse*)out_http_response);

    // Executing HTTP parser.
    size_t bytes_parsed = http_parser_execute(
        (http_parser *)&thread_http_response_parser,
        g_http_response_parser_settings,
        (const char *)response_buf,
        response_size_bytes);

    // Checking if we have complete data.
    if (!thread_http_response_parser.complete_header_flag_)
    {
        //std::cout << "Incomplete HTTP response headers supplied!" << std::endl;

        return SCERRAPPSHTTPPARSERINCOMPLETEHEADERS;
    }

    // Checking that all bytes are parsed.
    if (bytes_parsed != response_size_bytes)
    {
        //std::cout << "Provided HTTP response has incorrect data!" << std::endl;

        return SCERRAPPSHTTPPARSERINCORRECT;
    }

    HttpResponse* http_response = thread_http_response_parser.http_response_;

    // TODO: Check body length.

    // Setting response properties.
    http_response->response_offset_ = 0;
    http_response->response_len_bytes_ = response_size_bytes;

    // Setting status code.
    http_response->status_code_ = thread_http_response_parser.http_parser_.status_code;

    return 0;
}

// Initializes the internal Apps HTTP response parser.
uint32_t sc_init_http_response_parser()
{
    g_http_response_parser_settings = new http_parser_settings();

    // Setting HTTP callbacks.
    g_http_response_parser_settings->on_body = HttpResponseOnBody;
    g_http_response_parser_settings->on_header_field = HttpResponseOnHeaderField;
    g_http_response_parser_settings->on_header_value = HttpResponseOnHeaderValue;
    g_http_response_parser_settings->on_headers_complete = HttpResponseOnHeadersComplete;
    g_http_response_parser_settings->on_message_begin = HttpResponseOnMessageBegin;
    g_http_response_parser_settings->on_message_complete = HttpResponseOnMessageComplete;
    g_http_response_parser_settings->on_url = HttpResponseOnUri;

    return 0;
}

} // namespace network
} // namespace starcounter