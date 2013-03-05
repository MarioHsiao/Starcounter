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

inline int32_t MeasureSignedInt(char* puri, int offset, int32_t max_len)
{
    int32_t n = 0;
    switch (puri[offset])
    {
    case '-':
    case '+':
        {
            if (max_len < 2)
                return 0;

            if (IsDigit(puri[offset + 1]))
                n += 2;
            else
                return -1;

            break;
        }

    default:
        {
            if (IsDigit(puri[offset]))
                n++;
            else
                return -1;

            break;
        }
    }

    // Counting digits.
    while((n < max_len) && IsDigit(puri[offset + n]))
        n++;

    return n;
}

inline int32_t MeasureSignedDecimal(char* puri, int offset, int32_t max_len)
{
    int32_t n = 0;
    switch (puri[offset])
    {
    case '-':
    case '+':
    case '.':
        {
            if (max_len < 2)
                return 0;

            if (IsDigit(puri[offset + 1]))
                n += 2;
            else
                return -1;

            break;
        }

    default:
        {
            if (IsDigit(puri[offset]))
                n++;
            else
                return -1;

            break;
        }
    }

    // Counting digits.
    while((n < max_len) && (IsDigit(puri[offset + n]) || (puri[offset + n] == '.')))
        n++;

    return n;
}

inline int32_t MeasureBoolean(char* puri, int offset, int32_t max_len)
{
    // Skipping True.
    if (puri[offset] == 't' || puri[offset] == 'T')
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

inline int32_t MeasureDateTime(char* puri, int offset, int32_t max_len)
{
    // TODO
    return 0;
}

inline int32_t MeasureStringUntilWhitespace(char* puri, int offset, int32_t max_len)
{
    int32_t n = 0;
    while((n < max_len) && (!IsWhiteSpace(puri[offset + n])))
        n++;

    return n;
}

inline int32_t MeasureStringUntilSymbol(char* puri, int offset, int32_t max_len, char c)
{
    int32_t n = 0;
    while((n < max_len) && (puri[offset + n] != c))
        n++;

    return n;
}

class CodegenUriMatcher
{
    static const uint32_t MAX_URI_MATCHING_CODE_BYTES = 1024 * 1024;
    uint32_t uri_code_size_bytes_;
    char* uri_matching_code_;

    MixedCodeConstants::GenerateNativeUriMatcherType generate_uri_matcher_;

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
    uint32_t GenerateUriMatcher(MixedCodeConstants::RegisteredUriManaged* uri_infos, uint32_t num_uris)
    {
        uri_code_size_bytes_ = MAX_URI_MATCHING_CODE_BYTES;

        uint32_t err_code = 0;

        err_code = generate_uri_matcher_(uri_infos, num_uris, uri_matching_code_, &uri_code_size_bytes_);

        std::ifstream config_file_stream(L"codegen_uri_matcher.cpp");
        std::stringstream str_stream;
        str_stream << config_file_stream.rdbuf();
        std::string tmp_str = str_stream.str();
        strcpy_s(uri_matching_code_, tmp_str.size() + 1, tmp_str.c_str());

        return err_code;
    }

    // Compile given code into native dll.
    uint32_t CompileIfNeededAndLoadDll(
        UriMatchCodegenCompilerType comp_type,
        std::wstring gen_file_name,    
        MixedCodeConstants::MatchUriType* out_match_uri_func,
        HMODULE* out_codegen_dll_handle);

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