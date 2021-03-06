#include <windows.h>
#include <psapi.h>
#include <tlhelp32.h>
#include <string.h>
#include <cstdint>
#include <fstream>

#include "resource.h"

const int32_t MAX_PATH_LEN = 1024;

const wchar_t* Net45ResName = L"\\dotnetfx45_full_x86_x64.exe";
const wchar_t* Vs2015Redistx64ResName = L"\\vc_redist.x64.exe";
const wchar_t* Vs2015Redistx86ResName = L"\\vc_redist.x86.exe";
const wchar_t* ScSetupExtractName = L"\\Starcounter-Setup.exe";
const wchar_t* ScServiceName = L"\\scservice.exe";
const wchar_t* ScPersonalServerXmlName = L"\\personal.xml";
const wchar_t* ScVersion = L"2.0.0.0";
const wchar_t* ElevatedParam = L"Elevated";

const int32_t ERR_CANT_SHELLEXECUTE = 1;
const int32_t ERR_NO_STARCOUNTERBIN = 2;
const int32_t ERR_SCSERVICE_DOESNT_EXIST = 3;
const int32_t ERR_NO_TEMP_VARIABLE = 4;
const int32_t ERR_ANOTHER_SETUP_RUNNING = 5;
const int32_t ERR_CANT_INSTALL_DOTNET = 6;
const int32_t ERR_CANT_CREATE_TEMP_DIR = 7;

/// <summary>
/// Checks if the latest .NET 4.5 version is installed.
/// </summary>
/// <returns>True if yes.</returns>
static bool IsNet45Installed()
{
    const wchar_t* dot_net_sub_key = L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full";

    DWORD net_version;
    DWORD data_len = 4;

    LONG result = RegGetValue(HKEY_LOCAL_MACHINE, dot_net_sub_key, L"Release", RRF_RT_DWORD, NULL, &net_version, &data_len);
    if (ERROR_SUCCESS != result)
        return false;

    if (net_version >= 378389)
        return true;

    return false;
}

/// <summary>
/// Checks if CRT installed.
/// </summary>
/// <returns>True if yes.</returns>
static bool IsCRTInstalled(const wchar_t* key_path)
{
	DWORD value = 0;
	DWORD value_len = 4;

	LONG result = RegGetValue(HKEY_LOCAL_MACHINE, key_path, L"Installed", RRF_RT_DWORD, NULL, &value, &value_len);
	if (ERROR_SUCCESS == result) {
		if (1 == value) {
			return true;
		}
	}

	return false;
}

/// <summary>
/// Runs specified program and waits for it.
/// </summary>
/// <param name="exeFilePath"></param>
/// <param name="args"></param>
/// <returns></returns>
static int32_t RunAndWaitForProgram(const wchar_t* exe_path, const wchar_t* args, bool elevate, bool wait, bool show_window)
{
    SHELLEXECUTEINFO process_info = {0};
    process_info.cbSize = sizeof(process_info);
    process_info.fMask = SEE_MASK_NOCLOSEPROCESS;
    process_info.hwnd = 0;

    if (elevate)
        process_info.lpVerb = L"runas";

    process_info.lpFile = exe_path;
    process_info.lpParameters = args;
    process_info.lpDirectory = 0;
    
    if (show_window)
        process_info.nShow = SW_SHOW;
	 
    process_info.hInstApp = 0;

    if (ShellExecuteEx(&process_info))
    {
        DWORD exit_code = 0;
        if (wait)
        {
            // Wait until child process exits.
            WaitForSingleObject(process_info.hProcess, INFINITE);

            // Getting exit code of the process.
            GetExitCodeProcess(process_info.hProcess, &exit_code);

            return exit_code;
        }

        CloseHandle(process_info.hProcess);
    }

    return ERR_CANT_SHELLEXECUTE;
}

/// <summary>
/// Extracts certain resource file to disk.
/// </summary>
/// <param name="resourceFileName"></param>
/// <param name="pathToDestFile"></param>
static uint32_t ExtractResourceToFile(const int32_t resource_id, const wchar_t* dest_file_path)
{
    HRSRC res_handle = FindResource(NULL, MAKEINTRESOURCE(resource_id), L"EXE");
    DWORD res_size_bytes = SizeofResource(NULL, res_handle);
    HGLOBAL res_data_handle = LoadResource(NULL, res_handle);
    void* res_data = LockResource(res_data_handle);

    std::ofstream f(dest_file_path, std::ios::out | std::ios::binary);
    f.write((char*)res_data, res_size_bytes);
    f.close();

    return 0;
}

/// <summary>
/// Enabling debug privileges.
/// </summary>
void EnableDebugPriv()
{
    HANDLE hToken;
    LUID luid;
    TOKEN_PRIVILEGES tkp;

    OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);

    LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid);

    tkp.PrivilegeCount = 1;
    tkp.Privileges[0].Luid = luid;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    AdjustTokenPrivileges(hToken, false, &tkp, sizeof(tkp), NULL, NULL);

    CloseHandle(hToken); 
}

/// <summary>
/// Check if another instance of setup is running.
/// </summary>
static bool AnotherSetupRunning()
{
    PROCESSENTRY32 process_info;
    process_info.dwSize = sizeof(PROCESSENTRY32);

    wchar_t str_temp[256];
    DWORD current_pid = GetCurrentProcessId();

    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, NULL);

    if (Process32First(snapshot, &process_info) == TRUE)
    {
        while (Process32Next(snapshot, &process_info) == TRUE)
        {
            // Converting process name to lower case.
            size_t len = wcslen(process_info.szExeFile);
            wcscpy_s(str_temp, 256, process_info.szExeFile);
            _wcslwr_s(str_temp, 256);

            // Comparing with other process names.
            if ((0 == wcsncmp(str_temp, L"starcounter-", 12)) &&
                (process_info.th32ProcessID != current_pid))
            {
                CloseHandle(snapshot);
                return true;
            }
        }
    }

    CloseHandle(snapshot);

    return false;
}

/// <summary>
/// Starts Starcounter service.
/// </summary>
static uint32_t StartScService()
{
    wchar_t path_to_server_xml[MAX_PATH_LEN];
    wchar_t path_to_scservice[MAX_PATH_LEN];
    path_to_scservice[0] = '\0';
    DWORD data_len = MAX_PATH_LEN * sizeof(wchar_t);

    LONG result = RegGetValue(HKEY_CURRENT_USER, L"Environment", L"StarcounterBin", RRF_RT_REG_SZ, NULL, &path_to_scservice, &data_len);
    if (ERROR_SUCCESS != result)
        return ERR_NO_STARCOUNTERBIN;

    // Constructing path to server XML config.
    wcscpy_s(path_to_server_xml, MAX_PATH_LEN, path_to_scservice);
    wcscat_s(path_to_server_xml, MAX_PATH_LEN, ScPersonalServerXmlName);

    // Setting current process StarcounterBin.
    SetEnvironmentVariable(L"StarcounterBin", path_to_scservice);

    // Concatenating service EXE name.
    wcscat_s(path_to_scservice, MAX_PATH_LEN, ScServiceName);

    // Checking if EXE file exists.
    if ((INVALID_FILE_ATTRIBUTES == GetFileAttributes(path_to_server_xml)) && (GetLastError() == ERROR_FILE_NOT_FOUND) ||        
        (INVALID_FILE_ATTRIBUTES == GetFileAttributes(path_to_scservice)) && (GetLastError() == ERROR_FILE_NOT_FOUND))
    {
        return ERR_SCSERVICE_DOESNT_EXIST;
    }
    else
    {
        // Starting Starcounter service.
        RunAndWaitForProgram(path_to_scservice, L"", false, false, false);
    }

    return 0;
}

typedef BOOL (WINAPI *LPFN_ISWOW64PROCESS) (HANDLE, PBOOL);
LPFN_ISWOW64PROCESS fnIsWow64Process;

/// <summary>
/// Determines if we are running on 32-bit OS.
/// </summary>
BOOL IsWow64()
{
    BOOL bIsWow64 = FALSE;

    //IsWow64Process is not available on all supported versions of Windows.
    //Use GetModuleHandle to get a handle to the DLL that contains the function
    //and GetProcAddress to get a pointer to the function if available.

    fnIsWow64Process = (LPFN_ISWOW64PROCESS) GetProcAddress(
        GetModuleHandle(TEXT("kernel32")),"IsWow64Process");

    if(NULL != fnIsWow64Process)
    {
        if (!fnIsWow64Process(GetCurrentProcess(),&bIsWow64))
        {
            // handle error
        }
    }

    return bIsWow64;
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
{
    // Obtaining command line arguments.
    int32_t argc;
    wchar_t** argv = CommandLineToArgvW(pCmdLine, &argc);

    bool is_elevated = false;
    if (0 == wcscmp((const wchar_t*)argv[0], ElevatedParam))
        is_elevated = true;

    int32_t err_code = 0;
	const int32_t k_err_message_max_len = 1024;
	wchar_t err_message[k_err_message_max_len];
	swprintf_s(err_message, k_err_message_max_len, L"No specific reason.");

    // Simply exiting if another setup is running.
    if (!is_elevated)
    {
        // Checking for 64-bit Windows.
        if (!IsWow64())
        {
            MessageBox(
                NULL,
                L"Starcounter requires 64-bit version of operating system.",
                L"Starcounter setup...",
                MB_OK | MB_ICONERROR | MB_SYSTEMMODAL);

            return 0;
        }

        // Enabling some debug privileges to check other processes.
        EnableDebugPriv();

        // Checking if another setup is running.
        if (AnotherSetupRunning())
        {
            MessageBox(
                NULL,
                L"Another Starcounter setup instance is already running.",
                L"Starcounter setup...",
                MB_OK | MB_ICONEXCLAMATION | MB_SYSTEMMODAL);

            return ERR_ANOTHER_SETUP_RUNNING;
        }

        // Getting current EXE file path.
        wchar_t cur_exe_path[MAX_PATH_LEN];
        GetModuleFileName(NULL, cur_exe_path, MAX_PATH_LEN);
        
        // Running elevated wrapper.
        err_code = RunAndWaitForProgram(cur_exe_path, ElevatedParam, true, true, true);
        
        // Just returning error code.
        if (err_code)
            return err_code;

        // Starting Starcounter service.
        StartScService();

        return 0;
    }
    else
    {
        // Extracting everything to temp directory.
        wchar_t extract_temp_dir[MAX_PATH_LEN];
        int32_t num_chars = GetEnvironmentVariable(L"TEMP", extract_temp_dir, MAX_PATH_LEN);
        if ((num_chars <= 0) || (num_chars >= MAX_PATH_LEN))
            return ERR_NO_TEMP_VARIABLE;

        // Adding specific version sub-folder.
        wcscat_s(extract_temp_dir, MAX_PATH_LEN, L"\\");
        wcscat_s(extract_temp_dir, MAX_PATH_LEN, ScVersion);

        // Creating TEMP extract directory.
        if (!CreateDirectory(extract_temp_dir, NULL))
        {
			if (ERROR_ALREADY_EXISTS != GetLastError()) {

				swprintf_s(err_message, k_err_message_max_len, L"Can't create extraction temp directory.");

				err_code = ERR_CANT_CREATE_TEMP_DIR;
			}
        }

		// Checking if any errors occurred.
		if (err_code)
			goto SETUP_FAILED;

		// Checking if VS CRT is installed.
		if (!IsCRTInstalled(L"SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x86") ||
			!IsCRTInstalled(L"SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64"))
		{
			MessageBox(
				NULL,
				L"Microsoft Visual Studio 2015 Redistributable is not detected on your machine. It will be installed now. Thank you for your patience.",
				L"Starcounter setup...",
				MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);

			wchar_t temp_vs2015_redist_exe_path[MAX_PATH_LEN];
			wcscpy_s(temp_vs2015_redist_exe_path, MAX_PATH_LEN, extract_temp_dir);
			wcscat_s(temp_vs2015_redist_exe_path, MAX_PATH_LEN, Vs2015Redistx64ResName);

			// Extracting setup file.
			if (0 == (err_code = ExtractResourceToFile(IDR_VCREDIST_X64_EXE, temp_vs2015_redist_exe_path)))
			{
				if (0 != (err_code = RunAndWaitForProgram(temp_vs2015_redist_exe_path, L"/install /passive /norestart", true, true, true)))
				{
					// Redistributable installation succeeded but restart is required.
					if (3010 == err_code) {

						MessageBox(
							NULL,
							L"Installation of Microsoft Visual Studio 2015 Redistributable (x64) succeeded but requires system restart. Please restart your computer now and start Starcounter installation again.",
							L"Starcounter setup...",
							MB_OK | MB_ICONERROR | MB_SYSTEMMODAL);

						return err_code;
					}

					swprintf_s(err_message, k_err_message_max_len, L"Installation of Microsoft Visual Studio 2015 Redistributable (x64) failed.");
				}
			}
			else {

				swprintf_s(err_message, k_err_message_max_len, L"Extraction of Microsoft Visual Studio 2015 Redistributable (x64) setup failed.");
			}

			// Checking if any errors occurred.
			if (err_code)
				goto SETUP_FAILED;

			wcscpy_s(temp_vs2015_redist_exe_path, MAX_PATH_LEN, extract_temp_dir);
			wcscat_s(temp_vs2015_redist_exe_path, MAX_PATH_LEN, Vs2015Redistx86ResName);

			// Extracting setup file.
			if (0 == (err_code = ExtractResourceToFile(IDR_VCREDIST_X86_EXE, temp_vs2015_redist_exe_path)))
			{
				if (0 != (err_code = RunAndWaitForProgram(temp_vs2015_redist_exe_path, L"/install /passive /norestart", true, true, true)))
				{
					// Redistributable installation succeeded but restart is required.
					if (3010 == err_code) {

						MessageBox(
							NULL,
							L"Installation of Microsoft Visual Studio 2015 Redistributable (x86) succeeded but requires system restart. Please restart your computer now and start Starcounter installation again.",
							L"Starcounter setup...",
							MB_OK | MB_ICONERROR | MB_SYSTEMMODAL);

						return err_code;
					}

					swprintf_s(err_message, k_err_message_max_len, L"Installation of Microsoft Visual Studio 2015 Redistributable (x86) failed.");
				}
			}
			else {

				swprintf_s(err_message, k_err_message_max_len, L"Extraction of Microsoft Visual Studio 2015 Redistributable (x86) setup failed.");
			}

			// Checking if any errors occurred.
			if (err_code)
				goto SETUP_FAILED;
		}

        // Checking if .NET 4.5 is installed.
        if (!IsNet45Installed())
        {
            MessageBox(
                NULL,
                L"Microsoft .NET Framework 4.5 is not detected on your computer. It will be installed now.",
                L"Starcounter setup...",
                MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);

            wchar_t temp_net45_exe_path[MAX_PATH_LEN];
            wcscpy_s(temp_net45_exe_path, MAX_PATH_LEN, extract_temp_dir);
            wcscat_s(temp_net45_exe_path, MAX_PATH_LEN, Net45ResName);

            // Extracting setup file.
            if (0 == (err_code = ExtractResourceToFile(IDR_DOTNETSETUP_EXE, temp_net45_exe_path)))
            {
                if (0 == (err_code = RunAndWaitForProgram(temp_net45_exe_path, L"", true, true, true)))
                {
                    // Double checking that now .NET version has really installed.
					if (!IsNet45Installed()) {

						swprintf_s(err_message, k_err_message_max_len, L"Microsoft .NET Framework 4.5 seems not to be installed properly.");

						err_code = ERR_CANT_INSTALL_DOTNET;
					}
                }
				else {

					swprintf_s(err_message, k_err_message_max_len, L"Microsoft .NET Framework 4.5 produced errors during installation.");
				}
			}
			else {

				swprintf_s(err_message, k_err_message_max_len, L"Extraction of Microsoft .NET Framework 4.5 setup failed.");
			}
        }

        // Checking if any errors occurred.
        if (err_code)
            goto SETUP_FAILED;

        // Path to setup EXE in TEMP.
        wchar_t temp_setup_exe_path[MAX_PATH_LEN];
        wcscpy_s(temp_setup_exe_path, MAX_PATH_LEN, extract_temp_dir);
        wcscat_s(temp_setup_exe_path, MAX_PATH_LEN, ScSetupExtractName);

        // Extracting installer and starting it.
        if (0 == (err_code = ExtractResourceToFile(IDR_STARCOUNTER_SETUP_EXE, temp_setup_exe_path)))
        {
            // Skipping waiting for installer, just quiting.
            if (0 == (err_code = RunAndWaitForProgram(temp_setup_exe_path, L"DontCheckOtherInstances", true, true, true)))
            {
                // Cleaning temporary extract folder.
                // NOTE: last file should be double null-terminated.
                extract_temp_dir[wcslen(extract_temp_dir) + 1] = L'\0';
                SHFILEOPSTRUCT shfo = { NULL, FO_DELETE, extract_temp_dir, NULL, FOF_SILENT | FOF_NOERRORUI | FOF_NOCONFIRMATION, FALSE, NULL, NULL };
                SHFileOperation(&shfo);

                return 0;
            }
			else {
				swprintf_s(err_message, k_err_message_max_len, L"Internal Starcounter setup failed.");
			}
		}
		else {
			swprintf_s(err_message, k_err_message_max_len, L"Extraction of Starcounter setup failed.");
		}
    }

SETUP_FAILED:

    wchar_t err_str[2048];
    swprintf_s(err_str, 2048, L"Starcounter setup failed. Returned error code: %d. Error message: %s", err_code, err_message);

    MessageBox(
        NULL,
        err_str,
        L"Starcounter setup...",
        MB_OK | MB_ICONERROR | MB_SYSTEMMODAL);

    return err_code;
}