﻿#pragma once
#ifndef UTILITIES_HPP
#define UTILITIES_HPP

// Adding random generator.
#include "random.hpp"

namespace starcounter {
namespace network {

// Temporary big buffer.
const int32_t TEMP_BIG_BUFFER_SIZE = 4096;

// If you are debugging the gateway uncomment the following.
//#define GW_DEV_DEBUG

// Define if you want to log into Gateway specific log file.
//#define GW_LOG_TO_FILE

// Define if Gateway log should be printed to console as well.
/*
#ifdef GW_DEV_DEBUG
#define GW_LOG_TO_CONSOLE
#endif
*/

#define GW_TRACK_ALLOCATIONS

#ifdef GW_TRACK_ALLOCATIONS

extern int64_t g_NumAllocationsCounter;
extern int64_t g_NumAlignedAllocationsCounter;

#define GwNewArray(Type, Size) new Type[Size]; InterlockedIncrement64(&g_NumAllocationsCounter)
#define GwNewConstructor(Type) new Type(); InterlockedIncrement64(&g_NumAllocationsCounter)
#define GwNewConstructor1(Type, Param1) new Type(Param1); InterlockedIncrement64(&g_NumAllocationsCounter)
#define GwNewConstructor2(Type, Param1, Param2) new Type(Param1, Param2); InterlockedIncrement64(&g_NumAllocationsCounter)
#define GwDeleteArray(Ptr) delete[] Ptr; InterlockedDecrement64(&g_NumAllocationsCounter)
#define GwDeleteSingle(Ptr) delete Ptr; InterlockedDecrement64(&g_NumAllocationsCounter)

#define GwNewAligned(Size) _aligned_malloc(Size, MEMORY_ALLOCATION_ALIGNMENT); InterlockedIncrement64(&g_NumAlignedAllocationsCounter)
#define GwDeleteAligned(Ptr) _aligned_free(Ptr); InterlockedDecrement64(&g_NumAlignedAllocationsCounter)

#else

#define GwNewArray(Type, Size) (new Type[Size])
#define GwNewConstructor(Type) (new Type())
#define GwNewConstructor1(Type, Param1) (new Type(Param1))
#define GwNewConstructor2(Type, Param1, Param2) (new Type(Param1, Param2))
#define GwDeleteArray(Ptr) (delete[] Ptr)
#define GwDeleteSingle(Ptr) (delete Ptr)

#define GwNewAligned(Size) _aligned_malloc(Size, MEMORY_ALLOCATION_ALIGNMENT)
#define GwDeleteAligned(Ptr) _aligned_free(Ptr)
#endif

#if defined(GW_LOG_TO_FILE) || defined(GW_LOG_TO_CONSOLE)
#define GW_LOGGING_ON
#endif

enum GwLoggingType
{
    GW_LOGGING_ERROR_TYPE,
    GW_LOGGING_WARNING_TYPE,
    GW_LOGGING_NOTICE_TYPE,
    GW_LOGGING_CRITICAL_TYPE
};

class ServerLoggingSafe
{
public:

    ServerLoggingSafe(GwLoggingType t)
    {
        t_ = t;
    }

    // Writing to log once object is destroyed.
    ~ServerLoggingSafe();

    // Overloading all needed streaming operators.
    template <typename T> ServerLoggingSafe& operator<<(T const& t)
    {
        ss_ << t; // Accumulate into a non-shared stringstream, no threading issues.
        return *this;
    }

private:

    std::wstringstream ss_;
    GwLoggingType t_;
};

class CoutSafe
{
public:

    // Writing to log once object is destroyed.
    ~CoutSafe();

    // Overloading all needed streaming operators.
    template <typename T> CoutSafe& operator<<(T const& t)
    {
#ifdef GW_LOGGING_ON
        ss_ << t; // Accumulate into a non-shared stringstream, no threading issues.
#endif
        return *this;
    }

private:

#ifdef GW_LOGGING_ON
    std::stringstream ss_;
#endif
};

// Defining two streams output object.
#ifdef GW_LOGGING_ON
#define GW_COUT CoutSafe()
#else
#define GW_COUT if (false) CoutSafe()
#endif

#define GW_LOG_ERROR ServerLoggingSafe(GW_LOGGING_ERROR_TYPE)
#define GW_LOG_WARNING ServerLoggingSafe(GW_LOGGING_WARNING_TYPE)
#define GW_LOG_NOTICE ServerLoggingSafe(GW_LOGGING_NOTICE_TYPE)
#define GW_LOG_CRITICAL ServerLoggingSafe(GW_LOGGING_CRITICAL_TYPE)

#define GW_ENDL "\n"
#define GW_WENDL L"\n"

//uint64_t ReadDecimal(const char *start);
uint32_t PrintLastError(bool report_to_log = false);

// Reports statistics 
void ReportStatistics(const char* stat_name, const double stat_value);

// Invalid value of converted number from hexadecimal string.
const uint64_t INVALID_CONVERTED_NUMBER = 0xFFFFFFFFFFFFFFFF;

// Injects data into destination array.
static inline int32_t InjectData(uint8_t* const dest, int32_t dest_offset, const char* data, int32_t data_len_bytes)
{
    memcpy(dest + dest_offset, data, data_len_bytes);
    return dest_offset + data_len_bytes;
}

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

inline void replace_char_in_string(char *str_in, const char s, const char r) {
    
    int32_t t = static_cast<int32_t> (strlen(str_in));

    for (int32_t i = 0; i < t; i++) {
        if (str_in[i] == s)
            str_in[i] = r;
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
    std::cout << msg << ": " << time.wHour << "." << time.wMinute << "." << time.wSecond << "." << time.wMilliseconds << std::endl;
}

// Converts string to its uppercase copy.
inline std::string StringToUpperCopy(const std::string &src)
{
    std::string result;
    std::transform(src.begin(), src.end(), std::back_inserter(result), ::toupper);
    return result;
}

// Converts a part of string to lower case.
inline void StringToLower(
    char* dst,
    const char* src,
    const int32_t offset,
    const int32_t str_total_len) {

    // Copying first fragment before offset.
    memcpy(dst, src, offset);

    // Making URI lower case.
    for (int32_t i = offset; i < str_total_len; i++) {

        char c = src[i];

        // Lower casing only alphabetic letter.
        if ((c >= 'A') && (c <= 'Z')) {
            dst[i] = c | 32;
        } else {
            dst[i] = c;
        }
    }
}

} // namespace network
} // namespace starcounter

#endif // UTILITIES_HPP