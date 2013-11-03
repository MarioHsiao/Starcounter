
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>


extern DWORD _init_secattr_user(VOID *mem512);

DWORD _init_secattr_user(VOID *mem512)
{
    DWORD msize;
    BYTE *mem;
    SECURITY_ATTRIBUTES *psa;
    SECURITY_DESCRIPTOR *psd;
    PACL pAcl;
    DWORD cbAcl;
    DWORD dwNeeded;
    DWORD dwError;
    HANDLE hToken;
    PTOKEN_USER ptu;

    msize = 512;
    mem = (BYTE *)mem512;

    psa = (SECURITY_ATTRIBUTES *)mem; mem += sizeof(SECURITY_ATTRIBUTES);
    psd = (SECURITY_DESCRIPTOR *)mem; mem += sizeof(SECURITY_DESCRIPTOR);
    msize -= (sizeof(SECURITY_ATTRIBUTES) + sizeof(SECURITY_DESCRIPTOR));

    pAcl = NULL;
    cbAcl = 0;
    dwNeeded = 0;
    dwError = 0;

    if(!OpenProcessToken( GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        dwError = GetLastError();
        hToken = NULL;
        goto cleanup;
    }

    GetTokenInformation(hToken, TokenUser, NULL, 0, &dwNeeded);
    dwError = GetLastError();
    if(dwError != ERROR_INSUFFICIENT_BUFFER) 
    {
        goto cleanup;
    }
    dwError = 0;

    if (dwNeeded > msize)
    {
        dwError = ERROR_INSUFFICIENT_BUFFER;
        goto cleanup;
    }
    ptu = (TOKEN_USER *)mem; mem += dwNeeded;
    msize -= dwNeeded;

    if (GetTokenInformation(hToken, TokenUser, ptu, dwNeeded, &dwNeeded) == FALSE)
    {
        dwError = GetLastError();
        goto cleanup;
    }

    cbAcl = sizeof(ACL) + ((sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD)) + GetLengthSid(ptu->User.Sid));
    if (cbAcl > msize)
    {
        dwError = ERROR_INSUFFICIENT_BUFFER;
        goto cleanup;
    }
    pAcl = (ACL *)mem; msize += cbAcl;

    if(InitializeAcl(pAcl, cbAcl, ACL_REVISION) == FALSE)
    {
        dwError = GetLastError();
        goto cleanup;
    }

    if(AddAccessAllowedAce(pAcl,ACL_REVISION,GENERIC_ALL|STANDARD_RIGHTS_ALL|SPECIFIC_RIGHTS_ALL,ptu->User.Sid) == FALSE)
    {
        dwError = GetLastError();
        goto cleanup;
    }

    InitializeSecurityDescriptor(psd, SECURITY_DESCRIPTOR_REVISION);

    SetSecurityDescriptorDacl(psd, TRUE, pAcl, FALSE);
    SetSecurityDescriptorOwner(psd, ptu->User.Sid, FALSE);
    SetSecurityDescriptorGroup(psd, NULL, FALSE); 
    SetSecurityDescriptorSacl(psd, FALSE, NULL, FALSE);

    psa->nLength = sizeof(SECURITY_ATTRIBUTES);
    psa->lpSecurityDescriptor = psd;
    psa->bInheritHandle = FALSE;

cleanup:
    if (hToken != NULL) CloseHandle(hToken);
    SetLastError(dwError);
    return dwError;
}
