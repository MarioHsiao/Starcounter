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

// HTTP/WebSockets fields.
enum HttpWsFields
{
    GET_FIELD,
    POST_FIELD,
    COOKIE_FIELD,
    CONTENT_LENGTH_FIELD,
    ACCEPT_FIELD,
    ACCEPT_ENCODING_FIELD,
    COMPRESSION_FIELD,
    UPGRADE_FIELD,
    WS_KEY_FIELD,
    WS_VERSION_FIELD,
    WS_PROTOCOL_FIELD,
    UNKNOWN_FIELD
};

// Fast way to determine field type.
inline HttpWsFields DetermineField(const char *at, size_t length)
{
    switch(at[0])
    {
        case 'A':
        {
            switch(at[2])
            {
                case 'c':
                {
                    switch(length)
                    {
                        case 6: return ACCEPT_FIELD; // Accept
                        case 15: return ACCEPT_ENCODING_FIELD; // Accept-Encoding
                    }
                    break;
                }
            }

            break;
        }

        case 'C':
        {
            switch(at[2])
            {
                case 'o':
                {
                    switch(length)
                    {
                        case 6: return COOKIE_FIELD; // Cookie
                    }
                    break;
                }

                case 'n':
                {
                    switch(length)
                    {
                        case 14: return CONTENT_LENGTH_FIELD; // Content-Length
                    }
                    break;
                }
            }

            break;
        }

        case 'U':
        {
            switch(at[2])
            {
                case 'g':
                {
                    switch(length)
                    {
                        case 7: return UPGRADE_FIELD; // Upgrade
                    }
                    break;
                }
            }

            break;
        }
        
        case 'S':
        {
            switch(at[2])
            {
                case 'c':
                {
                    switch(length)
                    {
                        case 17: return WS_KEY_FIELD; // Sec-WebSocket-Key
                        case 22: return WS_PROTOCOL_FIELD; // Sec-WebSocket-Protocol
                        case 21: return WS_VERSION_FIELD; // Sec-WebSocket-Version
                    }
                    break;
                }
            }

            break;
        }
    }

    return UNKNOWN_FIELD;
}

// Parses decimal string into unsigned number.
inline uint32_t ParseDecimalStringToUint(const char *at, size_t length)
{
    uint32_t result = 0;
    int32_t mult = 1, i = (int32_t)length - 1;
    while (true)
    {
        result += (at[i] - '0') * mult;

        --i;
        if (i < 0)
            break;

        mult *= 10;
    }

    return result;
}

// Starcounter session string in HTTP.
const char* const kScSessionIdString = "ScSessionId";
const int32_t kScSessionIdStringLength = (int32_t)strlen(kScSessionIdString);

// Session string length in characters.
const int32_t SC_SESSION_STRING_LEN_CHARS = 24;

// Searching for the Starcounter session cookie among other cookies.
// Returns pointer to Starcounter session cookie value.
inline const char* GetSessionIdValue(const char *at, size_t length)
{
    int32_t i = 0;
    if (length >= kScSessionIdStringLength)
    {
        while(i < length)
        {
            // Checking if this cookie is Starcounter session cookie.
            if ((kScSessionIdString[0] == at[i]) &&
                (kScSessionIdString[1] == at[i + 1]) &&
                ('=' == at[i + kScSessionIdStringLength]))
            {
                // Skipping session header name and equality symbol.
                return at + i + kScSessionIdStringLength + 1;
            }
            i++;
        }
    }

    return NULL;
}

} // namespace network
} // namespace starcounter

#endif // HTTP_REQUEST_HPP
