#include <stdio.h>
#include <tchar.h>
#include <windows.h>
#include <winnls.h>
#include <shobjidl.h>
#include <objbase.h>
#include <objidl.h>
#include <shlguid.h>

// CreateLink - Uses the Shell's IShellLink and IPersistFile interfaces 
// to create and store a shortcut to the specified object. 
// Returns the result of calling the member functions of the interfaces. 
HRESULT CreateLink(
    LPCWSTR lpszPathOrigin,
    LPCWSTR lpszPathToLnk,
    LPCWSTR lpszArgs,
    LPCWSTR lpszWorkingDir,
    LPCWSTR lpszDesc,
    LPCWSTR lpszIconPath) 
{ 
    HRESULT hres; 
    IShellLink* psl; 

    // Get a pointer to the IShellLink interface. It is assumed that CoInitialize
    // has already been called.
    hres = CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_IShellLink, (LPVOID*)&psl); 
    if (SUCCEEDED(hres)) 
    { 
        IPersistFile* ppf; 

        // Set the path to the shortcut target and add the description. 
        psl->SetPath(lpszPathOrigin); 
        psl->SetDescription(lpszDesc);
        psl->SetArguments(lpszArgs);
        psl->SetWorkingDirectory(lpszWorkingDir);
        psl->SetIconLocation(lpszIconPath, 0);

        // Query IShellLink for the IPersistFile interface, used for saving the 
        // shortcut in persistent storage. 
        hres = psl->QueryInterface(IID_IPersistFile, (LPVOID*)&ppf); 

        if (SUCCEEDED(hres)) 
        { 
            // Save the link by calling IPersistFile::Save. 
            hres = ppf->Save(lpszPathToLnk, TRUE); 
            ppf->Release(); 
        } 
        psl->Release(); 
    } 
    return hres; 
}

int _tmain(int argc, _TCHAR* argv[])
{
    // Checking arguments number.
    if (argc < 6)
        return 1;

    CoInitialize(NULL);
    HRESULT hres = CreateLink(argv[1], argv[2], argv[3], argv[4], argv[5], argv[6]);
    CoUninitialize();
	return hres;
}

