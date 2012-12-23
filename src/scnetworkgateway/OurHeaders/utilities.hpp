﻿#pragma once
#ifndef UTILITIES_HPP
#define UTILITIES_HPP

// Adding random generator.
#include "random.hpp"

namespace starcounter {
namespace network {

// Defining two streams output object.
//#define GW_COUT std::cout
#define GW_COUT (*g_cout)
typedef boost::iostreams::tee_device<std::ostream, std::ofstream> TeeDevice;
typedef boost::iostreams::stream<TeeDevice> TeeLogStream;

// Logging object.
extern TeeLogStream *g_cout;

#if defined(UNICODE) || defined(_UNICODE)
#define tcout std::wcout
#else
#define tcout std::cout
#endif

//uint64_t ReadDecimal(const char *start);
uint32_t PrintLastError();

// Invalid value of converted number from hexadecimal string.
const uint64_t INVALID_CONVERTED_NUMBER = 0xFFFFFFFFFFFFFFFF;

// Converts uint64_t number to hexadecimal string.
inline int32_t uint64_to_hex_string(uint64_t number, char *str_out, int32_t num_4bits, bool null_string)
{
    char hex_table[16] = { '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F' };
    int32_t n = 0;
    while(number > 0)
    {
        str_out[n] = hex_table[number & 0xF];
        n++;
        number >>= 4;
    }

    // Filling with zero values if necessary.
    while (n < num_4bits)
    {
        str_out[n] = '0';
        n++;
    }

    // Checking if string should be null terminated.
    if (null_string)
        str_out[n] = '\0';

    // Returning length.
    return n;
}

inline void revert_string(char *str_in, int32_t len)
{
    int32_t half_len = len >> 1;
    for (int32_t i = 0; i < half_len; i++)
    {
        char c = str_in[i];
        str_in[i] = str_in[len - i - 1];
        str_in[len - i - 1] = c;
    }
}

// Converts hexadecimal string to uint64_t.
inline uint64_t hex_string_to_uint64(const char *str_in, int32_t num_4bits)
{
    uint64_t result = 0;
    int32_t i = 0, s = 0;

    for (int32_t n = 0; n < num_4bits; n++)
    {
        switch(str_in[i])
        {
            case '0': result |= ((uint64_t)0 << s); break;
            case '1': result |= ((uint64_t)1 << s); break;
            case '2': result |= ((uint64_t)2 << s); break;
            case '3': result |= ((uint64_t)3 << s); break;
            case '4': result |= ((uint64_t)4 << s); break;
            case '5': result |= ((uint64_t)5 << s); break;
            case '6': result |= ((uint64_t)6 << s); break;
            case '7': result |= ((uint64_t)7 << s); break;
            case '8': result |= ((uint64_t)8 << s); break;
            case '9': result |= ((uint64_t)9 << s); break;
            case 'A': result |= ((uint64_t)0xA << s); break;
            case 'B': result |= ((uint64_t)0xB << s); break;
            case 'C': result |= ((uint64_t)0xC << s); break;
            case 'D': result |= ((uint64_t)0xD << s); break;
            case 'E': result |= ((uint64_t)0xE << s); break;
            case 'F': result |= ((uint64_t)0xF << s); break;

            // INVALID_CONVERTED_NUMBER should never be returned in normal case.
            default: return INVALID_CONVERTED_NUMBER;
        }

        i++;
        s += 4;
    }

    return result;
}

inline uint32_t WriteUIntToString(char* buf, uint32_t value)
{
    uint32_t num_bytes = 0;

    // Checking for zero value.
    if (value < 10)
    {
        buf[0] = (char)'0' + value;
        return 1;
    }

    // Writing integers in reversed order.
    while (value != 0)
    {
        buf[num_bytes++] = (char)(value % 10 + '0');
        value = value / 10;
    }

    // Reversing the string.
    revert_string(buf, num_bytes);

    return num_bytes;
}

// Checking if one string starts after another.
inline uint32_t StartsWith(
    char* reg_uri,
    uint32_t reg_uri_chars,
    char* cur_uri,
    uint32_t cur_uri_chars,
    uint32_t skip_chars)
{
    uint32_t same_chars = skip_chars;

    while(reg_uri[same_chars] == cur_uri[same_chars])
    {
        if ((same_chars >= reg_uri_chars) ||
            (same_chars >= cur_uri_chars))
        {
            break;
        }

        same_chars++;
    }

    // Returning number of matched characters.
    return same_chars;
}

inline void PrintCurrentTimeMs(std::string msg)
{
    SYSTEMTIME time;
    GetSystemTime(&time);
    WORD millis = (time.wSecond * 1000) + time.wMilliseconds;
    std::cout << msg << ": " << millis << std::endl;
}

} // namespace network
} // namespace starcounter

#endif // UTILITIES_HPP