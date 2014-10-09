#pragma once
#ifndef HTTP_COMMON_HPP
#define HTTP_COMMON_HPP

#pragma warning(push)
#pragma warning(disable: 4163)
#include "../../../Chunks/bmx/bmx.hpp"
#pragma warning(pop)

#include "../../../Starcounter.Internal/Constants/MixedCodeConstants.cs"

namespace starcounter {
namespace network {

// HTTP/WebSockets fields.
enum HttpWsFields
{
    SCSESSIONID_FIELD,
    REFERRER_FIELD,
    XREFERRER_FIELD,
    COOKIE_FIELD,
    CONTENT_LENGTH_FIELD,
    ACCEPT_FIELD,
    ACCEPT_ENCODING_FIELD,
    UPGRADE_FIELD,
    WS_KEY_FIELD,
    WS_VERSION_FIELD,
    WS_PROTOCOL_FIELD,
    WS_EXTENSIONS_FIELD,
    WS_ACCEPT_FIELD,
    SCHEDULER_ID_FIELD,
    LOOP_HOST_FIELD,
    UNKNOWN_FIELD
};

// Fast way to determine field type.
inline HttpWsFields DetermineField(const char *at, size_t length)
{
    // NOTE: First checking assuming "standard" case sensitivity.
    switch (length) {

        case 6: { // Cookie

            if ((*(int32_t*)at == *(int32_t*)"Cookie") && (*(int32_t*) (at + 2) == *(int32_t*)"okie")) {
                return COOKIE_FIELD;
            }

            break;
        }

        case 7: { // Referer

            if ((*(int32_t*)at == *(int32_t*)"Upgrade") && (*(int32_t*)(at + 3) == *(int32_t*)"rade")) {
                return UPGRADE_FIELD;
            }

            if ((*(int32_t*)at == *(int32_t*)"Referer") && (*(int32_t*)(at + 3) == *(int32_t*)"erer")) {
                return REFERRER_FIELD;
            }

            break;
        }

        case 8: { // Loophost

            if ((*(int64_t*)at == *(int64_t*)"Loophost")) {
                return LOOP_HOST_FIELD;
            }

            break;
        }

        case 9: { // X-Referer

            if ((*(int64_t*)at == *(int64_t*)"X-Referer")) {
                return XREFERRER_FIELD;
            }

            break;
        }

        case 11: { // SchedulerId

            if ((*(int64_t*)at == *(int64_t*)"SchedulerId") && (*(int64_t*)(at + 3) == *(int64_t*)"edulerId")) {
                return SCHEDULER_ID_FIELD;
            }

            break;
        }

        case 14: { // Content-Length

            if ((*(int64_t*)at == *(int64_t*)"Content-Length") && (*(int64_t*) (at + 6) == *(int64_t*)"t-Length")) {
                return CONTENT_LENGTH_FIELD;
            }

            break;
        }

        case 15: { // Accept-Encoding

            if ((*(int64_t*)at == *(int64_t*)"Accept-Encoding") && (*(int64_t*)(at + 7) == *(int64_t*)"Encoding")) {
                return ACCEPT_ENCODING_FIELD;
            }

            break;
        }

        case 17: { // Sec-WebSocket-Key

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-Key") {
                return WS_KEY_FIELD;
            }

            break;
        }

        case 20: { // Sec-WebSocket-Accept

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-Acc") {
                return WS_ACCEPT_FIELD;
            }

            break;
        }

        case 21: { // Sec-WebSocket-Version

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-Ver") {
                return WS_VERSION_FIELD;
            }

            break;
        }

        case 22: { // Sec-WebSocket-Protocol

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-Pro") {
                return WS_PROTOCOL_FIELD;
            }

            break;
        }
                         
        case 24: { // Sec-WebSocket-Extensions

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-Ext") {
                return WS_EXTENSIONS_FIELD;
            }

            break;
        }

        default: {
            return UNKNOWN_FIELD;
        }
    }

    // Filtering too short and long headers.
    if ((length > 32) || (length < 4))
        return UNKNOWN_FIELD;

    // Now checking with case insensitivity.
    char temp[32];

    // Making the header lowercase (setting bit 6).
    for (size_t i = 0; i < length; i++) {
        temp[i] = at[i] | 32;
    }

    // Pointing to lower case temporary array.
    at = temp;

    switch (length) {

        case 6: { // Cookie

            if ((*(int32_t*)at == *(int32_t*)"cookie") && (*(int32_t*) (at + 2) == *(int32_t*)"okie")) {
                return COOKIE_FIELD;
            }

            break;
        }

        case 7: { // Referer

            if ((*(int32_t*)at == *(int32_t*)"upgrade") && (*(int32_t*)(at + 3) == *(int32_t*)"rade")) {
                return UPGRADE_FIELD;
            }

            if ((*(int32_t*)at == *(int32_t*)"referer") && (*(int32_t*)(at + 3) == *(int32_t*)"erer")) {
                return REFERRER_FIELD;
            }

            break;
        }

        case 8: { // Loophost

            if ((*(int64_t*)at == *(int64_t*)"loophost")) {
                return LOOP_HOST_FIELD;
            }

            break;
        }

        case 9: { // X-Referer

            if ((*(int64_t*)at == *(int64_t*)"x-referer")) {
                return XREFERRER_FIELD;
            }

            break;
        }

        case 11: { // SchedulerId

            if ((*(int64_t*)at == *(int64_t*)"schedulerid") && (*(int64_t*)(at + 3) == *(int64_t*)"edulerid")) {
                return SCHEDULER_ID_FIELD;
            }

            break;
        }

        case 14: { // Content-Length

            if ((*(int64_t*)at == *(int64_t*)"content-length") && (*(int64_t*) (at + 6) == *(int64_t*)"t-length")) {
                return CONTENT_LENGTH_FIELD;
            }

            break;
        }

        case 15: { // Accept-Encoding

            if ((*(int64_t*)at == *(int64_t*)"accept-encoding") && (*(int64_t*)(at + 7) == *(int64_t*)"encoding")) {
                return ACCEPT_ENCODING_FIELD;
            }

            break;
        }

        case 17: { // Sec-WebSocket-Key

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-key") {
                return WS_KEY_FIELD;
            }

            break;
        }

        case 20: { // Sec-WebSocket-Accept

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-acc") {
                return WS_ACCEPT_FIELD;
            }

            break;
        }

        case 21: { // Sec-WebSocket-Version

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-ver") {
                return WS_VERSION_FIELD;
            }

            break;
        }

        case 22: { // Sec-WebSocket-Protocol

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-pro") {
                return WS_PROTOCOL_FIELD;
            }

            break;
        }
                         
        case 24: { // Sec-WebSocket-Extensions

            if (*(int64_t*)(at + 9) == *(int64_t*)"cket-ext") {
                return WS_EXTENSIONS_FIELD;
            }

            break;
        }

        default: {
            return UNKNOWN_FIELD;
        }
    }
    
    return UNKNOWN_FIELD;
}

// Parses decimal string into unsigned number.
inline uint32_t ParseStringToUint(const char *at, size_t length)
{
    uint32_t result = 0;
    int32_t mult = 1, i = (int32_t)length - 1;
    while (i >= 0)
    {
        // Checking for white space character.
        switch (at[i])
        {
            case ' ':
            case '\n':
            case '\t':
            case '\r':
            {
                i--;
                continue;
            }
        }

        // Adding to result.
        result += (at[i] - '0') * mult;

        --i;
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
