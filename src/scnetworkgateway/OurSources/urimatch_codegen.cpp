#include "static_headers.hpp"
#include "gateway.hpp"
#include "handlers.hpp"
#include "ws_proto.hpp"
#include "http_proto.hpp"
#include "socket_data.hpp"
#include "tls_proto.hpp"
#include "worker_db_interface.hpp"
#include "worker.hpp"
#include "urimatch_codegen.hpp"

namespace starcounter {
namespace network {

// Initializes managed codegen loader.
void CodegenUriMatcher::Init()
{
    uri_matching_code_ = new char[MAX_URI_MATCHING_CODE_BYTES];

    // Loading managed URI matching codegen DLL.
    HINSTANCE rest_dll = LoadLibrary(L"Starcounter.Rest.dll");
    GW_ASSERT(rest_dll != NULL);

    generate_uri_matcher_ = (GenerateUriMatcherType) GetProcAddress(rest_dll, "GenerateNativeUriMatcher");
    GW_ASSERT(generate_uri_matcher_ != NULL);
}

// Compile given code into native dll.
uint32_t CodegenUriMatcher::CompileIfNeededAndLoadDll(
    UriMatchCodegenCompilerType comp_type,
    std::wstring out_name,
    MatchUriType& match_uri_func)
{
    switch(comp_type)
    {
        case COMPILER_MSVC:
        case COMPILER_GCC:
        {
            std::wstring out_dir = g_gateway.get_setting_gateway_output_dir() + L"\\rps";
            std::wstring out_cpp_path = out_dir + L"\\" + out_name + L".cpp";
            std::wstring out_dll_path = out_dir + L"\\" + out_name + L".dll";
            std::wstring compiler_output_path = out_dir + L"\\" + out_name + L".out";

            // Creating directory if it does not exist.
            if ((!CreateDirectory(out_dir.c_str(), NULL)) &&
                (ERROR_ALREADY_EXISTS != GetLastError()))
            {
                GW_ASSERT(false);                
            }

            // Checking if dll file already exists.
            std::ifstream dll_file(out_dll_path);
            if (dll_file.good())
                GW_ASSERT(false);

            std::wstring compiler_path;
            std::wstring compiler_cmd;

            if (comp_type == COMPILER_GCC)
            {
                compiler_path = L"\\MinGW\\bin\\x86_64-w64-mingw32-gcc.exe";
                compiler_cmd = std::wstring(L"\"") + g_gateway.get_setting_sc_bin_dir() + compiler_path + L"\" \"" + out_name + L".cpp" + L"\" -nostdlib -shared -O2 -o \"" + out_name + L".dll" + L"\"";
            }
            else
            {
                std::wstring msvc_path = L"C:\\Program Files (x86)\\Microsoft Visual Studio 11.0\\VC";
                std::wstring msvc_libs_dir = msvc_path + L"\\lib\\amd64;" + L"C:\\Program Files (x86)\\Windows Kits\\8.0\\Lib\\win8\\um\\x64";
                compiler_path = msvc_path + L"\\bin\\amd64\\cl.exe";

                SetEnvironmentVariable(L"LIB", msvc_libs_dir.c_str());
                SetEnvironmentVariable(L"Platform", L"x64");

                compiler_cmd = std::wstring(L"\"") + compiler_path + L"\" \"" + out_name + L".cpp" + L"\" /Od /Zi /LDd /MDd /link /SUBSYSTEM:WINDOWS /MACHINE:X64 /DEBUG /DLL";
            }

            // Saving code to file.
            //std::ofstream out_cpp_file = std::ofstream(out_cpp_path, std::ios::out | std::ios::app);
            //out_cpp_file.write(uri_matching_code_, uri_code_size_bytes_);
            //out_cpp_file.close();

            // Creating needed security attributes for compiler output file.
            SECURITY_ATTRIBUTES sa;
            sa.nLength = sizeof(SECURITY_ATTRIBUTES);
            sa.bInheritHandle = TRUE;
            sa.lpSecurityDescriptor = NULL;

            // Creating file for compiler output.
            HANDLE comp_output_file = CreateFile(compiler_output_path.c_str(), GENERIC_WRITE, 0, &sa,
                CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

            GW_ASSERT(comp_output_file != INVALID_HANDLE_VALUE);

            STARTUPINFO si;
            ZeroMemory(&si, sizeof(STARTUPINFO));
            si.cb = sizeof(STARTUPINFO);

            // Redirecting compiler output.
            si.hStdError = comp_output_file;
            si.hStdOutput = comp_output_file;
            si.hStdInput = INVALID_HANDLE_VALUE;
            si.dwFlags = STARTF_USESTDHANDLES;

            // Starting compiler process.
            PROCESS_INFORMATION pi;
            bool br = CreateProcess(NULL, (LPWSTR)compiler_cmd.c_str(), NULL, NULL,
                TRUE, CREATE_NO_WINDOW, NULL, out_dir.c_str(), &si, &pi);

            GW_ASSERT(true == br);

            DWORD err_code = WaitForSingleObject(pi.hProcess, INFINITE);
            GW_ASSERT(err_code == WAIT_OBJECT_0);

            // Getting the exit code of the compiler.
            br = GetExitCodeProcess(pi.hProcess, &err_code);

            // Closing handles.
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            CloseHandle(comp_output_file);

            GW_ASSERT(true == br);
            if (0 != err_code)
            {
                // Opening file stream.
                std::ifstream compiler_output_file_stream(compiler_output_path.c_str());
                bool file_opened = compiler_output_file_stream.is_open();
                GW_ASSERT(file_opened);

                // Reading compiler output contents into string stream.
                std::stringstream str_stream;
                str_stream << compiler_output_file_stream.rdbuf();
                std::string tmp_str = str_stream.str();
                uint32_t num_chars = tmp_str.size();

                // COnverting char to wchar_t basically.
                wchar_t* wcstring = new wchar_t[num_chars + 1];
                size_t num_conv_chars = 0;
                mbstowcs_s(&num_conv_chars, wcstring, num_chars, tmp_str.c_str(), _TRUNCATE);
                GW_ASSERT(num_chars == num_conv_chars);

                // Copying config contents into a string.
                std::wstringstream comp_output_wchar_stream;
                comp_output_wchar_stream << L"URI matcher generated code compilation has failed:" << std::endl;
                comp_output_wchar_stream << wcstring;
                delete [] wcstring;

                // Sending compiler output to server log.
                g_gateway.LogWriteError(comp_output_wchar_stream.str().c_str());

                GW_ASSERT(false);
            }

            // Load generated library into memory and load procedure by name.
            HINSTANCE codegen_dll = LoadLibrary(out_dll_path.c_str());
            GW_ASSERT(codegen_dll != NULL);

            match_uri_func = (MatchUriType) GetProcAddress(codegen_dll, "Process");
            GW_ASSERT(match_uri_func != NULL);

            break;
        }
        
        case COMPILER_CLANG:
        {
            // TODO!
            break;
        }
    }

    return 0;
}


} // namespace network
} // namespace starcounter