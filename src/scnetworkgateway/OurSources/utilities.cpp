#include "static_headers.hpp"
#include "utilities.hpp"

namespace starcounter {
namespace network {

static CRITICAL_SECTION* StatisticsCS = NULL;
static char BuildNumber[32];
static bool IsPersonal = false;
static bool IsDebugBuild = false;
static bool IsRunningOnBuildServer = false;
static char MachineName[128];
static wchar_t BuildStatisticsFilePath[1024];

// Reports statistics 
void ReportStatistics(const char* stat_name, const double stat_value)
{
    char temp_str[512];

    // Doing statistics initialization once.
    if (NULL == StatisticsCS)
    {
        GetEnvironmentVariableA("SC_RUNNING_ON_BUILD_SERVER", temp_str, 64);
        if (0 == strcmp(temp_str, "True"))
            IsRunningOnBuildServer = true;
        else
            return; // If we are not on build server - just returning.

        GetEnvironmentVariableA("BUILD_NUMBER", BuildNumber, 32);

        DWORD size = 128;
        GetComputerNameA(MachineName, &size);

        GetEnvironmentVariableA("BUILD_IS_PERSONAL", temp_str, 64);
        if (0 == strcmp(temp_str, "True"))
            IsPersonal = true;

        GetEnvironmentVariableA("Configuration", temp_str, 64);
        if (0 == strcmp(temp_str, "Debug"))
            IsDebugBuild = true;

        DWORD data_len = 1024 * sizeof(wchar_t);

        LONG result = RegGetValue(HKEY_CURRENT_USER, L"Environment", L"TEMP", RRF_RT_REG_SZ, NULL, &BuildStatisticsFilePath, &data_len);
        wcscat_s(BuildStatisticsFilePath, 1024, L"\\ScBuildStatistics.txt");

        StatisticsCS = new CRITICAL_SECTION;
        InitializeCriticalSection(StatisticsCS);
    }

    time_t now = time(0); // Get time now
    tm t;
    localtime_s(&t, &now);
    char cur_date[64];
    sprintf_s(cur_date, 64, "%d-%d-%dT%d:%d:%d",
        1900 + t.tm_year,
        t.tm_mon,
        t.tm_mday,
        t.tm_hour,
        t.tm_min,
        t.tm_sec);

    sprintf_s(temp_str, 512, "%s %s %.2f %s %s %s\n",
        stat_name,
        BuildNumber,
        stat_value,
        IsPersonal ? "PERSONAL" : "PUBLIC",
        cur_date,
        MachineName);

    EnterCriticalSection(StatisticsCS);

    printf(temp_str);
    std::ofstream build_stats_file;

    if (IsRunningOnBuildServer)
    {
        build_stats_file.open(BuildStatisticsFilePath, std::ios_base::app);
        build_stats_file << temp_str; 
        build_stats_file.close();
    }

    LeaveCriticalSection(StatisticsCS);
}

// Reads decimal from the given string.
// (reads until characters are digits)
/*uint64_t ReadDecimal(const char *start)
{
    if (!isdigit(*start))
        return -1;

    const char *origStart = start;
    while (isdigit(*start))
        start++;

    uint64_t degree = 1, res = 0;
    start--;
    while(1)
    {
        res += ((*start) - '0') * degree;
        degree *= 10;

        if (start != origStart)
            start--;
        else
            break;
    }

    return res;
}*/

// Print error code.
uint32_t PrintLastError(bool report_to_log)
{
    const int32_t max_err_msg_len = 512;

    // Retrieve the system error message for the last-error code.
    TCHAR *err_buf;
    uint32_t dw = GetLastError();

    // Getting system error message.
    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER |
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        dw,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &err_buf,
        0,
        NULL);

    // Copying the error message.
    TCHAR err_display_buf[max_err_msg_len];
    StringCchPrintf(err_display_buf, max_err_msg_len, TEXT("Error %d: %s"), dw, err_buf); 

    // Converting wchar_t to char.
    char err_display_buf_char[max_err_msg_len];
    wcstombs(err_display_buf_char, err_display_buf, max_err_msg_len);

    // Printing error to console/log.
    GW_COUT << err_display_buf_char << GW_ENDL;

    // Printing error to server log.
    if (report_to_log)
        GW_LOG_ERROR << err_display_buf_char << GW_WENDL;

    // Free message resources.
    LocalFree(err_buf);

    return dw;
}

} // namespace network
} // namespace starcounter
