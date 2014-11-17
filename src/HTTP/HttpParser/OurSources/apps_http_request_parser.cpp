#include "http_parser.h"
#include "http_request.hpp"

namespace starcounter {
namespace network {

// Structure that is used during parsing.
struct HttpRequestParserStruct
{
    // An http_parser instance inside.
    http_parser http_parser_;

    // Structure that holds HTTP request.
    HttpRequest* http_request_;

    // HTTP/WebSockets fields.
    HttpWsFields last_field_;

    // Indicates if we have complete header.
    bool complete_header_flag_;

    // Checks if X-Referer field is read.
    bool is_xhreferer_read_;

    // Pointer to request buffer.
    uint8_t* request_buf_;

    // Resets the parser related fields.
    void Reset(uint8_t* request_buf, HttpRequest* http_request)
    {
        request_buf_ = request_buf;
        http_request_ = http_request;

        last_field_ = UNKNOWN_FIELD;
        http_request_->Reset();

        http_parser_init(&http_parser_, HTTP_REQUEST);
        complete_header_flag_ = false;
        is_xhreferer_read_ = false;
    }
};

// Global HTTP parser settings.
http_parser_settings* g_http_request_parser_settings = NULL;

// Declaring thread-safe parser structure.
__declspec(thread) HttpRequestParserStruct thread_http_request_parser;

inline int HttpRequestOnMessageBegin(http_parser* p)
{
    // Do nothing.
    return 0;
}

inline int HttpRequestOnHeadersComplete(http_parser* p)
{
    HttpRequestParserStruct *http = (HttpRequestParserStruct *)p;

    // Setting complete header flag.
    http->complete_header_flag_ = true;

    // Setting headers length (skipping 2 bytes for \r\n).
    http->http_request_->headers_len_bytes_ = p->nread - 2 - http->http_request_->headers_len_bytes_;

    return 0;
}

inline int HttpRequestOnMessageComplete(http_parser* p)
{
    // Do nothing.
    return 0;
}

inline int HttpRequestOnUri(http_parser* p, const char *at, size_t length)
{
    HttpRequestParserStruct *http = (HttpRequestParserStruct *)p;

    // Setting the reference to URI.
    http->http_request_->uri_offset_ = (uint16_t)(at - (char*)http->request_buf_);
    http->http_request_->uri_len_bytes_ = (uint16_t)length;

    return 0;
}

inline int HttpRequestOnHeaderField(http_parser* p, const char *at, size_t length)
{
    HttpRequestParserStruct *http = (HttpRequestParserStruct *)p;

    // Determining what header field is that.
    http->last_field_ = DetermineField(at, length);

    // Setting headers beginning.
    if (!http->http_request_->headers_offset_)
    {
        http->http_request_->headers_len_bytes_ = (uint16_t)(p->nread - length - 1);
        http->http_request_->headers_offset_ = (uint16_t)(at - (char*)http->request_buf_);
    }

    return 0;
}

inline int HttpRequestOnHeaderValue(http_parser* p, const char *at, size_t length)
{
    HttpRequestParserStruct *http = (HttpRequestParserStruct *)p;

    // Processing last field type.
    switch (http->last_field_)
    {
        case CONTENT_LENGTH_FIELD:
        {
            // Calculating body length.
            http->http_request_->content_len_bytes_ = (int32_t) p->content_length;

            break;
        }

        case ACCEPT_ENCODING_FIELD:
        {
            // Checking if Gzip is accepted.
            size_t i = 0;
            while (i < length)
            {
                if (at[i] == 'g')
                {
                    if (at[i + 1] == 'z')
                    {
                        http->http_request_->gzip_accepted_ = true;
                        break;
                    }
                }
                i++;
            }

            break;
        }

        case REFERRER_FIELD:
        {
            // Do nothing if X-Referer field is already processed.
            if (http->is_xhreferer_read_)
                break;
        }

        case XREFERRER_FIELD:
        {
            // Checking if Starcounter session id is presented.
            if (MixedCodeConstants::SESSION_STRING_LEN_CHARS == length)
            {
                // Setting the session offset.
                http->http_request_->session_string_offset_ = (uint16_t)(at - (char*)http->request_buf_);

                // Checking if X-Referer field is read.
                if (XREFERRER_FIELD == http->last_field_)
                    http->is_xhreferer_read_ = true;
            }

            break;
        }
    }

    return 0;
}

inline int HttpRequestOnBody(http_parser* p, const char *at, size_t length)
{
    HttpRequestParserStruct *http = (HttpRequestParserStruct *)p;

    // Setting body parameters.
    if (http->http_request_->content_len_bytes_ <= 0)
        http->http_request_->content_len_bytes_ = (uint16_t)length;

    // Setting body data offset.
    http->http_request_->content_offset_ = (uint16_t)(at - (char*)http->request_buf_);

    return 0;
}

// Parses HTTP request from the given buffer and returns corresponding instance of HttpRequest.
EXTERN_C uint32_t __stdcall sc_parse_http_request(
    uint8_t* request_buf,
    uint32_t request_size_bytes,
    uint8_t* out_http_request)
{
    assert(g_http_request_parser_settings != NULL);

    // Resetting the parser structure.
    thread_http_request_parser.Reset(request_buf, (HttpRequest*)out_http_request);

    // Executing HTTP parser.
    size_t bytes_parsed = http_parser_execute(
        &thread_http_request_parser.http_parser_,
        g_http_request_parser_settings,
        (const char *)request_buf,
        request_size_bytes);

    // Checking if we have complete data.
    if (!thread_http_request_parser.complete_header_flag_)
    {
        //std::cout << "Incomplete HTTP request headers supplied!" << std::endl;

        return SCERRAPPSHTTPPARSERINCOMPLETEHEADERS;
    }

    // Checking that all bytes are parsed.
    if (bytes_parsed != request_size_bytes)
    {
        //std::cout << "Provided HTTP request has incorrect data!" << std::endl;

        return SCERRAPPSHTTPPARSERINCORRECT;
    }

    HttpRequest* http_request = thread_http_request_parser.http_request_;

    // Checking for special case when body is not yet received.
    if (http_request->content_offset_ <= 0)
    {
        if (http_request->content_len_bytes_ > 0)
            return SCERRAPPSHTTPPARSERINCOMPLETEHEADERS;
    }

    // Getting the HTTP method.
    http_method method = (http_method)thread_http_request_parser.http_parser_.method;
    switch (method)
    {
    case http_method::HTTP_GET: 
        http_request->http_method_ = bmx::HTTP_METHODS::GET_METHOD;
        break;

    case http_method::HTTP_POST: 
        http_request->http_method_ = bmx::HTTP_METHODS::POST_METHOD;
        break;

    case http_method::HTTP_PUT: 
        http_request->http_method_ = bmx::HTTP_METHODS::PUT_METHOD;
        break;

    case http_method::HTTP_DELETE: 
        http_request->http_method_ = bmx::HTTP_METHODS::DELETE_METHOD;
        break;

    case http_method::HTTP_HEAD: 
        http_request->http_method_ = bmx::HTTP_METHODS::HEAD_METHOD;
        break;

    case http_method::HTTP_OPTIONS: 
        http_request->http_method_ = bmx::HTTP_METHODS::OPTIONS_METHOD;
        break;

    case http_method::HTTP_TRACE: 
        http_request->http_method_ = bmx::HTTP_METHODS::TRACE_METHOD;
        break;

    case http_method::HTTP_PATCH: 
        http_request->http_method_ = bmx::HTTP_METHODS::PATCH_METHOD;
        break;

    default: 
        http_request->http_method_ = bmx::HTTP_METHODS::OTHER_METHOD;
        break;
    }

    // TODO: Check body length.

    // Setting request properties (+2 for \r\n between headers and body).
    http_request->request_len_bytes_ = http_request->headers_offset_ + http_request->headers_len_bytes_ + 2 + http_request->content_len_bytes_;

    return 0;
}

// Initializes the internal Apps HTTP request parser.
uint32_t sc_init_http_request_parser()
{
    g_http_request_parser_settings = new http_parser_settings();

    // Setting HTTP callbacks.
    g_http_request_parser_settings->on_body = HttpRequestOnBody;
    g_http_request_parser_settings->on_header_field = HttpRequestOnHeaderField;
    g_http_request_parser_settings->on_header_value = HttpRequestOnHeaderValue;
    g_http_request_parser_settings->on_headers_complete = HttpRequestOnHeadersComplete;
    g_http_request_parser_settings->on_message_begin = HttpRequestOnMessageBegin;
    g_http_request_parser_settings->on_message_complete = HttpRequestOnMessageComplete;
    g_http_request_parser_settings->on_url = HttpRequestOnUri;

    return 0;
}

} // namespace network
} // namespace starcounter