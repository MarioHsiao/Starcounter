#pragma once
#ifndef URIMATCH_CODEGEN_HPP
#define URIMATCH_CODEGEN_HPP

namespace starcounter {
namespace network {
enum UriMatchCodegenCompilerType
{
    COMPILER_GCC,
    COMPILER_MSVC,
    COMPILER_CLANG
};

struct UserParameterInfo
{
    uint16_t offset;
    uint16_t len_bytes;
    uint8_t data_type;
};

inline bool IsDigit(char c)
{
    return (c >= '0') && (c <= '9');
}

inline bool IsWhiteSpace(char c)
{
    return (c == ' ') || (c == '\n') || (c == '\t');
}

inline int32_t SkipSignedInteger(char* start, int32_t max_len)
{
    if (max_len < 1)
        return 0;

    int32_t n = 0;
    switch (start[0])
    {
        case '-':
        case '+':
        {
            if (max_len < 2)
                return 0;

            if (IsDigit(start[1]))
                n += 2;
            else
                return 0;

            break;
        }
        
        default:
        {
            if (IsDigit(start[0]))
                n++;
            else
                return 0;

            break;
        }
    }

    // Counting digits.
    while((n < max_len) && IsDigit(start[n]))
        n++;

    return n;
}

inline int32_t SkipSignedDecimal(char* start, int32_t max_len)
{
    if (max_len < 1)
        return 0;

    int32_t n = 0;
    switch (start[0])
    {
        case '-':
        case '+':
        case '.':
        {
            if (max_len < 2)
                return 0;

            if (IsDigit(start[1]))
                n += 2;
            else
                return 0;

            break;
        }

        default:
        {
            if (IsDigit(start[0]))
                n++;
            else
                return 0;

            break;
        }
    }

    // Counting digits.
    while((n < max_len) && (IsDigit(start[n]) || (start[n] == '.')))
        n++;

    return n;
}

inline int32_t SkipBoolean(char* start, int32_t max_len)
{
    if (max_len < 4)
        return 0;

    // Skipping True.
    if (start[0] == 't' || start[0] == 'T')
    {
        // TODO: Check for remaining part.

        return 4;
    }
    else // Assuming False.
    {
        if (max_len < 5)
            return 0;

        // TODO: Check for remaining part.

        return 5;
    }

    return 0;
}

inline int32_t SkipDateTime(char* start)
{
    // TODO
    return 0;
}

inline int32_t SkipStringUntilWhiteSpace(char* start, int32_t max_len)
{
    if (max_len < 1)
        return 0;

    int32_t n = 0;
    while((n < max_len) && (!IsWhiteSpace(start[n])))
        n++;

    return n;
}

inline int32_t SkipStringUntilSymbol(char* start, char c, int32_t max_len)
{
    if (max_len < 1)
        return 0;

    int32_t n = 0;
    while((n < max_len) && (start[n] != c))
        n++;

    return n;
}

class CodegenUriMatcher
{
    static const uint32_t MAX_URI_MATCHING_CODE_BYTES = 1024 * 1024;
    uint32_t uri_code_size_bytes_;
    char* uri_matching_code_;

    typedef uint32_t (*GenerateUriMatcherType) (
        RegisteredUriManaged* uri_infos,
        uint32_t num_uris,
        char* gen_code_str,
        uint32_t* gen_code_str_num_bytes
        );

    GenerateUriMatcherType generate_uri_matcher_;

public:

    CodegenUriMatcher()
    {
        generate_uri_matcher_ = NULL;
        uri_matching_code_ = NULL;
        uri_code_size_bytes_ = 0;
    }

    // Getting URI matching code.
    char* get_uri_matching_code()
    {
        return uri_matching_code_;
    }

    // Initializes managed codegen loader.
    void Init();

    // Generate the code using managed generator.
    uint32_t GenerateUriMatcher(RegisteredUriManaged* uri_infos, uint32_t num_uris)
    {
        uri_code_size_bytes_ = MAX_URI_MATCHING_CODE_BYTES;
        return generate_uri_matcher_(uri_infos, num_uris, uri_matching_code_, &uri_code_size_bytes_);
    }

    // Compile given code into native dll.
    uint32_t CompileIfNeededAndLoadDll(
        UriMatchCodegenCompilerType comp_type,
        std::wstring out_name,
        MatchUriType& latest_match_uri);

    ~CodegenUriMatcher()
    {
        if (uri_matching_code_)
        {
            delete [] uri_matching_code_;
            uri_matching_code_ = NULL;
        }
    }
};
} // namespace network
} // namespace starcounter

#endif // URIMATCH_CODEGEN_HPP