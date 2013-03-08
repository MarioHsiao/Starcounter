#pragma once
#ifndef HTTP_COMMON_HPP
#define HTTP_COMMON_HPP

#include "../../../Chunks/bmx/bmx.hpp"
#include "../../../Starcounter.Internal/Constants/MixedCodeConstants.cs"

namespace starcounter {
namespace network {

// Starcounter session string in HTTP.
const char* const kScSessionIdStringWithExtraChars = "ScSsnId: ";
const int32_t kScSessionIdStringWithExtraCharsLength = (int32_t)strlen(kScSessionIdStringWithExtraChars);

const char* const kFullSessionIdString = "ScSsnId: ########################";
const char* const kFullSessionIdSetCookieString = "Set-Cookie: ScSsnId=########################\r\n";
const int32_t kFullSessionIdSetCookieStringLength = (int32_t)strlen(kFullSessionIdSetCookieString);

const int32_t kSetCookieStringPrefixLength = (int32_t)strlen("Set-Cookie: ScSsnId=");

const int32_t kFullSessionIdStringLength = (int32_t)strlen(kFullSessionIdString);

// Session string length in characters.
const int32_t SC_SESSION_STRING_LEN_CHARS = 24;
const int32_t SC_SESSION_STRING_INDEX_LEN_CHARS = 8;
const int32_t SC_SESSION_STRING_SALT_LEN_CHARS = SC_SESSION_STRING_LEN_CHARS - SC_SESSION_STRING_INDEX_LEN_CHARS;

// HTTP/WebSockets fields.
enum HttpWsFields
{
    GET_FIELD,
    POST_FIELD,
    COOKIE_FIELD,
    SET_COOKIE_FIELD,
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
const int64_t SET_COOKIE_HEADER_VALUE_8BYTES = 7741528618789266771;
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

        case SET_COOKIE_HEADER_VALUE_8BYTES:
        {
            return SET_COOKIE_FIELD; // Set-Cookie
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

#endif // HTTP_COMMON_HPP
