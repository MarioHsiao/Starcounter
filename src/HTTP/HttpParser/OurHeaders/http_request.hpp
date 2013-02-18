#pragma once
#ifndef HTTP_REQUEST_HPP
#define HTTP_REQUEST_HPP

#include "../../../Chunks/bmx/bmx.hpp"

namespace starcounter {
namespace network {

// Maximum number of pre-parsed HTTP headers.
const int32_t MAX_HTTP_HEADERS = 16;

// Starcounter session string in HTTP.
const char* const kScSessionIdStringWithExtraChars = "ScSsnId: ";
const int32_t kScSessionIdStringWithExtraCharsLength = (int32_t)strlen(kScSessionIdStringWithExtraChars);
const char* const kScFullSessionIdString = "ScSsnId: ########################";
const int32_t kScFullSessionIdStringLength = (int32_t)strlen(kScFullSessionIdString);

// Session string length in characters.
const int32_t SC_SESSION_STRING_LEN_CHARS = 24;
const int32_t SC_SESSION_STRING_INDEX_LEN_CHARS = 8;
const int32_t SC_SESSION_STRING_SALT_LEN_CHARS = SC_SESSION_STRING_LEN_CHARS - SC_SESSION_STRING_INDEX_LEN_CHARS;

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
    SCSESSIONID_FIELD,
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

const int64_t ACCEPT_HEADER_VALUE_8BYTES = 2322296583949083457;
const int64_t ACCEPT_ENCODING_HEADER_VALUE_8BYTES = 4984768388655178561;
const int64_t COOKIE_HEADER_VALUE_8BYTES = 2322280061311348547;
const int64_t CONTENT_LENGTH_HEADER_VALUE_8BYTES = 3275364211029339971;
const int64_t UPGRADE_HEADER_VALUE_8BYTES = 4207879796541583445;
const int64_t WEBSOCKET_HEADER_VALUE_8BYTES = 6008476277963711827;
const int64_t SCSESSIONID_HEADER_VALUE_8BYTES = 4207568690600960851;

// Fast way to determine field type.
inline HttpWsFields DetermineField(const char *at, size_t length)
{
    int64_t header_8bytes = *(int64_t*)at;
    switch(header_8bytes)
    {
        case ACCEPT_HEADER_VALUE_8BYTES:
        {
            return ACCEPT_FIELD; // Accept
        }

        case ACCEPT_ENCODING_HEADER_VALUE_8BYTES:
        {
            if (*(int64_t*)(at + 7) == *(int64_t*)"Encoding")
                return ACCEPT_ENCODING_FIELD; // Accept-Encoding

            break;
        }

        case COOKIE_HEADER_VALUE_8BYTES:
        {
            return COOKIE_FIELD; // Cookie
        }

        case CONTENT_LENGTH_HEADER_VALUE_8BYTES:
        {
            if (*(int64_t*)(at + 8) == *(int64_t*)"Length: ")
                return CONTENT_LENGTH_FIELD; // Content-Length

            break;
        }

        case UPGRADE_HEADER_VALUE_8BYTES:
        {
            return UPGRADE_FIELD; // Upgrade
        }
        
        case WEBSOCKET_HEADER_VALUE_8BYTES:
        {
            switch(length)
            {
                case 17: return WS_KEY_FIELD; // Sec-WebSocket-Key
                case 21: return WS_VERSION_FIELD; // Sec-WebSocket-Version
                case 22: return WS_PROTOCOL_FIELD; // Sec-WebSocket-Protocol
            }

            break;
        }

        case SCSESSIONID_HEADER_VALUE_8BYTES:
        {
            return SCSESSIONID_FIELD;
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

// Searching for the Starcounter session cookie among other cookies.
// Returns pointer to Starcounter session cookie value.
/*inline const char* GetSessionIdValueString(const char *at, size_t length)
{
    // Immediately excluding wrong cookie.
    if (length < kScFullSessionIdStringLength)
        return NULL;

    // Null terminating the string.
    char* atnull = (char*)at;
    char c = atnull[length];
    atnull[length] = '\0';

    // Searching using optimized strstr.
    const char* session_string = strstr(atnull, kScSessionIdStringPlus);

    // Restoring back the old character.
    atnull[length] = c;

    // Checking if session was found.
    if (session_string)
        return session_string + kScSessionIdStringPlusLength;

    return NULL;
}*/

} // namespace network
} // namespace starcounter

#endif // HTTP_REQUEST_HPP
