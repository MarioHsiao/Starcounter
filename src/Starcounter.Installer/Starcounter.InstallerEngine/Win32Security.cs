using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Starcounter.Management.Win32;

namespace Starcounter.Management.Win32
{
public static class Win32Security
{
    //       If no Command line arguments are specified, names are looked up
    //       on the local machine. If argv[1] is present, the lookup occurs
    //       on the specified machine.
    //
    //       For example, acctname.exe \\winbase will look up names from the
    //       machine named \\winbase. If \\winbase is a default German install
    //       of Windows NT, names will appear in German locale.
    //
    //   Author:
    //
    //       Scott Field (sfield)    02-Oct-96

    #region AccessRights enum

    [Flags]
    public enum AccessRights : uint
    {
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,

        STANDARD_RIGHTS_REQUIRED = 0x000F0000,

        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,

        STANDARD_RIGHTS_ALL = 0x001F0000,

        SPECIFIC_RIGHTS_ALL = 0x0000FFFF,

        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000
    }

    #endregion

    #region LSA_AccessPolicy enum

    public enum LSA_AccessPolicy : long
    {
        POLICY_VIEW_LOCAL_INFORMATION = 0x00000001L,
        POLICY_VIEW_AUDIT_INFORMATION = 0x00000002L,
        POLICY_GET_PRIVATE_INFORMATION = 0x00000004L,
        POLICY_TRUST_ADMIN = 0x00000008L,
        POLICY_CREATE_ACCOUNT = 0x00000010L,
        POLICY_CREATE_SECRET = 0x00000020L,
        POLICY_CREATE_PRIVILEGE = 0x00000040L,
        POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080L,
        POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100L,
        POLICY_AUDIT_LOG_ADMIN = 0x00000200L,
        POLICY_SERVER_ADMIN = 0x00000400L,
        POLICY_LOOKUP_NAMES = 0x00000800L,
        POLICY_NOTIFICATION = 0x00001000L
    }

    #endregion

    #region SID_NAME_USE enum

    public enum SID_NAME_USE
    {
        SidTypeUser = 1,
        SidTypeGroup,
        SidTypeDomain,
        SidTypeAlias,
        SidTypeWellKnownGroup,
        SidTypeDeletedAccount,
        SidTypeInvalid,
        SidTypeUnknown,
        SidTypeComputer
    }

    #endregion

    #region TokenAccesses enum

    [Flags]
    public enum TokenAccesses
    {
        STANDARD_RIGHTS_REQUIRED = 0x000F0000,
        STANDARD_RIGHTS_READ = 0x00020000,
        TOKEN_ASSIGN_PRIMARY = 0x0001,
        TOKEN_DUPLICATE = 0x0002,
        TOKEN_IMPERSONATE = 0x0004,
        TOKEN_QUERY = 0x0008,
        TOKEN_QUERY_SOURCE = 0x0010,
        TOKEN_ADJUST_PRIVILEGES = 0x0020,
        TOKEN_ADJUST_GROUPS = 0x0040,
        TOKEN_ADJUST_DEFAULT = 0x0080,
        TOKEN_ADJUST_SESSIONID = 0x0100,
        TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY),
        TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
        TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
        TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
        TOKEN_ADJUST_SESSIONID),
    }

    #endregion

    private const Int32 ANYSIZE_ARRAY = 1;
    private const int bufferSize = 256;
    private const int ERROR_NOT_ALL_ASSIGNED = 1300;
    private const int RTN_ERROR = 13;

    private const int RTN_OK = 0;
    private const int RTN_USAGE = 1;

    [DllImport("advapi32", SetLastError = true)]
    private static extern bool AccessCheck(
        IntPtr pSecurityDescriptor,
        IntPtr ClientToken,
        TokenAccesses DesiredAccess,
        [In] ref GenericRightMapping GenericMapping,
        IntPtr PrivilegeSet,
        ref int PrivilegeSetLength,
        out int GrantedAccess,
        out bool AccessStatus);

    [DllImport("advapi32", SetLastError = true)]
    internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string StringSecurityDescriptor,
        int StringSDRevision,
        out IntPtr SecurityDescriptor,
        out int SecurityDescriptorSize
    );

    [DllImport("advapi32", SetLastError = true, EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW"
              )
    ]
    internal static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
        IntPtr SecurityDescriptor,
        int RequestedStringSDRevision,
        SECURITY_INFORMATION SecurityInformation,
        out IntPtr StringSecurityDescriptor,
        out int StringSecurityDescriptorLen
    );


    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenThreadToken(
        IntPtr ThreadHandle,
        TokenAccesses DesiredAccess,
        bool OpenAsSelf,
        out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle,
                                                TokenAccesses DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ImpersonateSelf(SECURITY_IMPERSONATION_LEVEL ImpersonationLevel);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool RevertToSelf();

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool MakeAbsoluteSD(
        IntPtr pSelfRelativeSD,
        IntPtr pAbsoluteSD,
        ref int lpdwAbsoluteSDSize,
        IntPtr pDacl,
        ref int lpdwDaclSize,
        IntPtr pSacl,
        ref int lpdwSaclSize,
        IntPtr pOwner,
        ref int lpdwOwnerSize,
        IntPtr pPrimaryGroup,
        ref int lpdwPrimaryGroupSize
    );

    [DllImport("advapi32.dll")]
    private static extern Win32Error GetEffectiveRightsFromAcl(
        IntPtr pacl,
        IntPtr pTrustee,
        out AccessRights pAccessRights
    );

    [DllImport("advapi32.dll")]
    private static extern Win32Error GetExplicitEntriesFromAcl(
        IntPtr pacl,
        out int pcCountOfExplicitEntries,
        out IntPtr pListOfExplicitEntries
    );

    [DllImport("advapi32.dll")]
    private static extern Win32Error SetEntriesInAcl(
        int cCountOfExplicitEntries,
        IntPtr pListOfExplicitEntries,
        IntPtr OldAcl,
        out IntPtr NewAcl
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CheckTokenMembership(
        IntPtr TokenHandle,
        IntPtr SidToCheck,
        out bool IsMember
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool AddAccessAllowedAce(
        IntPtr pAcl,
        int dwAceRevision,
        AccessRights AccessMask,
        IntPtr pSid
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool InitializeAcl(
        IntPtr pAcl,
        int nAclLength,
        int dwAclRevision
    );


    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct ACL
    {
        public byte AclRevision;
        public byte Sbz1;
        public short AclSize;
        public short AceCount;
        public short Sbz2;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool InitializeSecurityDescriptor(
        IntPtr pSecurityDescriptor,
        int dwRevision
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool SetSecurityDescriptorDacl(
        IntPtr pSecurityDescriptor,
        bool bDaclPresent,
        IntPtr pDacl,
        bool bDaclDefaulted
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool PrivilegeCheck(
        IntPtr ClientToken,
        ref PRIVILEGE_SET RequiredPrivileges,
        out bool pfResult);


    public static bool[] CheckPrivilegesForCurrentUser(string[] privileges)
    {
        if (!ImpersonateSelf(SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
        {
            throw Win32Kernel.CreateWin32Exception("Error invoking ImpersonateSelf.");
        }
        try
        {
            IntPtr hThreadToken;
            if (!OpenThreadToken(Win32Kernel.GetCurrentThread(),
                                 TokenAccesses.TOKEN_READ | TokenAccesses.TOKEN_ADJUST_PRIVILEGES, true,
                                 out hThreadToken))
            {
                throw Win32Kernel.CreateWin32Exception("Error invoking OpenProcessToken.");
            }
            try
            {
                bool[] results = new bool[privileges.Length];
                for (int i = 0; i < privileges.Length; i++)
                {
                    LUID luid;
                    if (!LookupPrivilegeValue(null, privileges[i], out luid))
                    {
                        throw Win32Kernel.CreateWin32Exception("Error invoking LookupPrivilegeValue.");
                    }
                    PRIVILEGE_SET privilegeSet = new PRIVILEGE_SET
                    {
                        Control = PRIVILEGE_SET_CONTROL.PRIVILEGE_SET_ALL_NECESSARY,
                        PrivilegeCount = 1,
                        Privilege = new[]
                        {
                            new LUID_AND_ATTRIBUTES
                            {
                                Attributes =
                                PRIVILEGE_SET_ATTRIBUTES.
                                SE_PRIVILEGE_NONE,
                                Luid = luid
                            }
                        }
                    };
                    bool result;
                    if (!PrivilegeCheck(hThreadToken, ref privilegeSet, out result))
                    {
                        throw Win32Kernel.CreateWin32Exception("Error invoking PrivilegeCheck.");
                    }
                    if (result)
                    {
                        results[i] = true;
                    }
                    else
                    {
                        TOKEN_PRIVILEGES newPrivileges = new TOKEN_PRIVILEGES
                        {
                            PrivilegeCount = 1,
                            Privileges = new[]
                            {
                                new LUID_AND_ATTRIBUTES
                                {
                                    Attributes =
                                    PRIVILEGE_SET_ATTRIBUTES
                                    .
                                    SE_PRIVILEGE_ENABLED,
                                    Luid = luid
                                }
                            }
                        };
                        int newPrivilegesBufferSize = Marshal.SizeOf(newPrivileges);
                        IntPtr newPrivilegesBuffer = Marshal.AllocHGlobal(newPrivilegesBufferSize);
                        int oldPrivilegesBufferSize = newPrivilegesBufferSize;
                        IntPtr oldPrivilegesBuffer = Marshal.AllocHGlobal(oldPrivilegesBufferSize);
                        Marshal.StructureToPtr(newPrivileges, newPrivilegesBuffer, false);
                        try
                        {
                            if (
                                !AdjustTokenPrivileges(hThreadToken, false, newPrivilegesBuffer,
                                                       newPrivilegesBufferSize, oldPrivilegesBuffer,
                                                       ref oldPrivilegesBufferSize))
                            {
                                int error = Marshal.GetLastWin32Error();
                                if (error != (int) Win32Error.ERROR_ACCESS_DENIED)
                                {
                                    throw Win32Kernel.CreateWin32Exception(error,
                                                                           "Error invoking AdjustTokenPrivileges.");
                                }
                            }
                            else
                            {
                                TOKEN_PRIVILEGES oldPrivileges =
                                    (TOKEN_PRIVILEGES)
                                    Marshal.PtrToStructure(oldPrivilegesBuffer, typeof(TOKEN_PRIVILEGES));
                                if (oldPrivileges.PrivilegeCount == 1)
                                {
                                    results[i] = true;
                                    try
                                    {
                                        // Restore.
                                        if (
                                            !AdjustTokenPrivileges(hThreadToken, false, oldPrivilegesBuffer,
                                                                   oldPrivilegesBufferSize, newPrivilegesBuffer,
                                                                   ref newPrivilegesBufferSize))
                                        {
                                            throw Win32Kernel.CreateWin32Exception(
                                                "Error invoking AdjustTokenPrivileges.");
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.DestroyStructure(oldPrivilegesBuffer, typeof(TOKEN_PRIVILEGES));
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Marshal.DestroyStructure(newPrivilegesBuffer, typeof(TOKEN_PRIVILEGES));
                            Marshal.FreeHGlobal(newPrivilegesBuffer);
                            Marshal.FreeHGlobal(oldPrivilegesBuffer);
                        }
                    }
                }
                return results;
            }
            finally
            {
                Win32Kernel.CloseHandle(hThreadToken);
            }
        }
        finally
        {
            RevertToSelf();
        }
    }

    public static AccessRights GetAccessRightsForCurrentUser(string stringSecurityDescriptor)
    {
        if (!ImpersonateSelf(SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
        {
            throw Win32Kernel.CreateWin32Exception("Error invoking ImpersonateSelf.");
        }
        try
        {
            IntPtr hThreadToken;
            if (!OpenThreadToken(Win32Kernel.GetCurrentThread(),
                                 TokenAccesses.TOKEN_READ, true, out hThreadToken))
            {
                throw Win32Kernel.CreateWin32Exception("Error invoking OpenProcessToken.");
            }
            try
            {
                IntPtr securityDescriptor;
                int securityDescriptorSize;
                if (!ConvertStringSecurityDescriptorToSecurityDescriptor(stringSecurityDescriptor, 1,
                                                                         out securityDescriptor,
                                                                         out securityDescriptorSize))
                {
                    throw Win32Kernel.CreateWin32Exception(
                        "Error invoking ConvertStringSecurityDescriptorToSecurityDescriptor.");
                }
                try
                {
                    // Make the security descriptor absolute.
                    IntPtr absoluteSecurityDescriptor = IntPtr.Zero;
                    IntPtr dacl = IntPtr.Zero;
                    IntPtr sacl = IntPtr.Zero;
                    IntPtr owner = IntPtr.Zero;
                    IntPtr primaryGroup = IntPtr.Zero;
                    int absoluteSecurityDescriptorSize = 0;
                    int daclSize = 0;
                    int saclSize = 0;
                    int ownerSize = 0;
                    int primaryGroupSize = 0;
                    MakeAbsoluteSD(securityDescriptor, IntPtr.Zero, ref absoluteSecurityDescriptorSize,
                                   IntPtr.Zero, ref daclSize, IntPtr.Zero, ref saclSize,
                                   IntPtr.Zero, ref ownerSize, IntPtr.Zero, ref primaryGroupSize);
                    absoluteSecurityDescriptor = Marshal.AllocHGlobal(absoluteSecurityDescriptorSize);
                    dacl = Marshal.AllocHGlobal(daclSize);
                    sacl = Marshal.AllocHGlobal(saclSize);
                    owner = Marshal.AllocHGlobal(ownerSize);
                    primaryGroup = Marshal.AllocHGlobal(primaryGroupSize);
                    if (!MakeAbsoluteSD(securityDescriptor, absoluteSecurityDescriptor,
                                        ref absoluteSecurityDescriptorSize,
                                        dacl, ref daclSize, sacl, ref saclSize, owner, ref ownerSize,
                                        primaryGroup, ref primaryGroupSize))
                    {
                        throw Win32Kernel.CreateWin32Exception("Error invoking MakeAbsoluteSD.");
                    }
                    try
                    {
                        // We should remove every SID that cannot be mapped to an account name.
                        int dacEntries;
                        IntPtr dacEntriesBuffer;
                        Win32Error win32Error = GetExplicitEntriesFromAcl(dacl, out dacEntries,
                                                                          out dacEntriesBuffer);
                        if (win32Error != Win32Error.ERROR_SUCCESS)
                        {
                            throw Win32Kernel.CreateWin32Exception("Error invoking GetExplicitEntriesFromAcl.");
                        }
                        try
                        {
                            unsafe
                            {
                                EXPLICIT_ACCESS * pEntry = (EXPLICIT_ACCESS *) dacEntriesBuffer;
                                AccessRights permissions = 0;
                                for (int i = 0; i < dacEntries; i++)
                                {
                                    bool member;
                                    if (pEntry->Trustee.TrusteeForm == TRUSTEE_FORM.TRUSTEE_IS_SID)
                                    {
                                        if (
                                            !CheckTokenMembership(hThreadToken, pEntry->Trustee.ptstrName,
                                        out member))
                                        {
                                            throw Win32Kernel.CreateWin32Exception(
                                                "Error invoking CheckTokenMembership.");
                                        }
                                    }
                                    else if (pEntry->Trustee.TrusteeForm == TRUSTEE_FORM.TRUSTEE_IS_NAME)
                                    {
                                        IntPtr sid;
                                        int sidSize = 0;
                                        string accountName = Marshal.PtrToStringUni(pEntry->Trustee.ptstrName);
                                        int domainNameSize = 0;
                                        SID_NAME_USE sidType;
                                        LookupAccountName(null, accountName, IntPtr.Zero, ref sidSize,
                                        null, ref domainNameSize, out sidType);
                                        sid = Marshal.AllocHGlobal(sidSize);
                                        StringBuilder domainName = new StringBuilder(domainNameSize);
                                        try
                                        {
                                            if (!LookupAccountName(null, accountName, sid, ref sidSize,
                                            domainName, ref domainNameSize, out sidType))
                                            {
                                                continue;
                                            }
                                            if (!CheckTokenMembership(hThreadToken, sid, out member))
                                            {
                                                throw Win32Kernel.CreateWin32Exception(
                                                    "Error invoking CheckTokenMembership.");
                                            }
                                        }
                                        finally
                                        {
                                            Marshal.FreeHGlobal(sid);
                                        }
                                    }
                                    else
                                    {
                                        // Not supported.
                                        continue;
                                    }
                                    if (member)
                                    {
                                        switch (pEntry->grfAccessMode)
                                        {
                                            case ACCESS_MODE.DENY_ACCESS:
                                            case ACCESS_MODE.SET_ACCESS:
                                                permissions = permissions & ~pEntry->grfAccessPermissions;
                                                break;
                                            case ACCESS_MODE.GRANT_ACCESS:
                                            case ACCESS_MODE.REVOKE_ACCESS:
                                                permissions = permissions | pEntry->grfAccessPermissions;
                                                break;
                                        }
                                    }
                                    pEntry++;
                                }

                                return permissions;


#if FALSE
                                StringBuilder nameBuffer = new StringBuilder(0);
                                StringBuilder domainBuffer = new StringBuilder(0);

                                for (int i = 0; i < dacEntries; i++)
                                {
                                    if (pEntry->Trustee.TrusteeForm == TRUSTEE_FORM.TRUSTEE_IS_SID)
                                    {
                                        int nameSize = nameBuffer.Capacity;
                                        int domainSize = domainBuffer.Capacity;
                                        SID_NAME_USE sidType;
                                        win32Error = Win32Error.ERROR_SUCCESS;
                                        if (!LookupAccountSid(null,
                                        pEntry->Trustee.ptstrName, nameBuffer, ref nameSize,
                                        domainBuffer, ref domainSize, out sidType))
                                        {
                                            win32Error = (Win32Error) Marshal.GetLastWin32Error();
                                        }
                                        if (win32Error == Win32Error.ERROR_INSUFFICIENT_BUFFER)
                                        {
                                            nameBuffer.EnsureCapacity(nameSize);
                                            domainBuffer.EnsureCapacity(domainSize);
                                            if (!LookupAccountSid(null,
                                            pEntry->Trustee.ptstrName, nameBuffer, ref nameSize,
                                            domainBuffer, ref domainSize, out sidType))
                                            {
                                                win32Error = (Win32Error) Marshal.GetLastWin32Error();
                                            }
                                        }
                                        if (win32Error == Win32Error.ERROR_NONE_MAPPED)
                                        {
                                            // Invalid SID. This entry will make GetEffectiveRightsFromAcl fail!
                                            // We should delete it.
                                            EXPLICIT_ACCESS * pEntryCopy = pEntry;
                                            for (int j = i; j < dacEntries; j++)
                                            {
                                                pEntryCopy++;
                                                *(pEntryCopy - 1) = * pEntryCopy;
                                            }
                                            dacEntries--;
                                        }
                                        else if (win32Error != Win32Error.ERROR_SUCCESS)
                                        {
                                            throw Win32Kernel.CreateWin32Exception((int) win32Error, "Error invoking LookupAccountSid.");
                                        }
                                        if (sidType == SID_NAME_USE.SidTypeAlias)
                                        {
                                            int sidSize = 0;
                                            // We have to resolve the alias.
                                            LookupAccountName(null, domainBuffer + "\\" + nameBuffer.ToString(),
                                            IntPtr.Zero, ref sidSize, null,
                                            ref domainSize, out sidType);
                                            win32Error = (Win32Error) Marshal.GetLastWin32Error();
                                            if (win32Error != Win32Error.ERROR_INSUFFICIENT_BUFFER)
                                            {
                                                throw Win32Kernel.CreateWin32Exception((int) win32Error, "Error invoking LookupAccountName.");
                                            }
                                            IntPtr sid = Marshal.AllocHGlobal(sidSize);
                                            domainBuffer.EnsureCapacity(domainSize);
                                            if (!LookupAccountName(null, nameBuffer.ToString(),
                                            sid, ref sidSize, domainBuffer,
                                            ref domainSize, out sidType))
                                            {
                                                throw Win32Kernel.CreateWin32Exception("Error invoking LookupAccountName.");
                                            }
                                            pEntry->Trustee.ptstrName = sid;
                                        }
                                        pEntry->Trustee.TrusteeType = (TRUSTEE_TYPE) sidType;
                                    }
                                    pEntry++;
                                }



                                // Rewrite the ACL.
                                Marshal.FreeHGlobal(dacl);
                                dacl = IntPtr.Zero;

                                win32Error = SetEntriesInAcl(dacEntries, dacEntriesBuffer, IntPtr.Zero,
                                out dacl);

                                if (win32Error != Win32Error.ERROR_SUCCESS)
                                {
                                    throw Win32Kernel.CreateWin32Exception("Error invoking SetEntriesInAcl.");
                                }
#endif
                            }
                        }
                        finally
                        {
                            Win32Kernel.LocalFree(dacEntriesBuffer);
                        }
                    }
                    finally
                    {
                        if (absoluteSecurityDescriptor != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(absoluteSecurityDescriptor);
                        }
                        if (dacl != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(dacl);
                        }
                        if (sacl != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(sacl);
                        }
                        if (owner != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(owner);
                        }
                        if (primaryGroup != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(primaryGroup);
                        }
                    }
#if  NONE
                    TRUSTEE trustee = new TRUSTEE
                    {
                        MultipleTrusteeOperation =
                        MULTIPLE_TRUSTEE_OPERATION.NO_MULTIPLE_TRUSTEE,
                        pMultipleTrustee = IntPtr.Zero,
                        TrusteeForm = TRUSTEE_FORM.TRUSTEE_IS_NAME,
                        TrusteeType = TRUSTEE_TYPE.TRUSTEE_IS_USER,
                        ptstrName = Marshal.StringToHGlobalUni(WindowsIdentity.GetCurrent().Name)
                    };
                    try
                    {
                        int trusteeBufferSize = Marshal.SizeOf(trustee);
                        IntPtr trusteeBuffer = Marshal.AllocHGlobal(trusteeBufferSize);
                        Marshal.StructureToPtr(trustee, trusteeBuffer, false);
                        try
                        {
                            AccessRights accessRights;
                            win32Error = GetEffectiveRightsFromAcl(dacl, trusteeBuffer, out accessRights);
                            if (win32Error != Win32Error.ERROR_SUCCESS)
                                throw Win32Kernel.CreateWin32Exception((int) win32Error,
                                                                       "Error invoking GetEffectiveRightsFromAcl.");
                            return accessRights != 0;
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(trusteeBuffer);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(trustee.ptstrName);
                    }
#endif
                }
                finally
                {
                    Win32Kernel.LocalFree(securityDescriptor);
                }
            }
            finally
            {
                Win32Kernel.CloseHandle(hThreadToken);
            }
        }
        finally
        {
            RevertToSelf();
        }
    }


    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
                                                     [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
                                                     IntPtr NewState,
                                                     int BufferLength,
                                                     IntPtr PreviousState,
                                                     ref int ReturnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AllocateAndInitializeSid(
        IntPtr pIdentifierAuthority,
        byte nSubAuthorityCount,
        int dwSubAuthority0, int dwSubAuthority1,
        int dwSubAuthority2, int dwSubAuthority3,
        int dwSubAuthority4, int dwSubAuthority5,
        int dwSubAuthority6, int dwSubAuthority7,
        out IntPtr pSid);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupAccountSid(
        string lpSystemName,
        IntPtr pSid,
        StringBuilder lpName,
        ref int cchName,
        StringBuilder ReferencedDomainName,
        ref int cchReferencedDomainName,
        out SID_NAME_USE peUse);

    [DllImport("advapi32.dll")]
    private static extern IntPtr FreeSid(IntPtr pSid);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall,
               SetLastError = true)]
    private static extern uint NetUserModalsGet(string server, int level, out IntPtr BufPtr);

    [DllImport("advapi32.dll")]
    private static extern IntPtr GetSidSubAuthorityCount(IntPtr pSid);

    [DllImport("advapi32.dll")]
    private static extern uint GetSidLengthRequired(byte nSubAuthorityCount);


    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeSid(IntPtr Sid, IntPtr pIdentifierAuthority, byte nSubAuthorityCount);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern IntPtr GetSidIdentifierAuthority(IntPtr pSid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern IntPtr GetSidSubAuthority(IntPtr pSid, uint nSubAuthority);


    [DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
    private static extern uint NetApiBufferFree(IntPtr buffer);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
                                                    out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeName(
        string lpSystemName,
        [In] ref LUID lpLuid,
        StringBuilder lpName,
        ref int cchName);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeDisplayName(
        string lpSystemName,
        string lpName,
        StringBuilder lpDisplayName,
        ref int cchName,
        out int languageId);

    [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
    private static extern uint LsaOpenPolicy(
        ref LSA_UNICODE_STRING SystemName,
        ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
        uint DesiredAccess,
        out IntPtr PolicyHandle
    );

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool ConvertSidToStringSid(IntPtr Sid, out string StringSid);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr Sid);

    public static string LookupPrivilegeDisplayName(string privilege)
    {
        int bufferSize = 1024;
        StringBuilder buffer = new StringBuilder(bufferSize);
        int languageId;
        if (!LookupPrivilegeDisplayName(null, privilege, buffer, ref bufferSize, out languageId))
        {
            throw Win32Kernel.CreateWin32Exception("Error invoking LookupPrivilegeDisplayName.");
        }
        return buffer.ToString();
    }

    public static string LookupAliasFromRid(string targetComputer, int rid)
    {
        unsafe
        {
            IntPtr pSid;
            SID_IDENTIFIER_AUTHORITY sia = WellKnownSid.SECURITY_NT_AUTHORITY;
            //PSID pSid;
            StringBuilder domainName = new StringBuilder(bufferSize);
            StringBuilder name = new StringBuilder(bufferSize);
            int domainNameSize = bufferSize;
            int nameSize = bufferSize;
            bool success = false;

            //
            // Sid is the same regardless of machine, since the well-known
            // BUILTIN domain is referenced.
            //

            if (AllocateAndInitializeSid(
                new IntPtr( & sia),
                2,
                WellKnownRid.SECURITY_BUILTIN_DOMAIN_RID,
                rid,
                0, 0, 0, 0, 0, 0,
                out pSid
            ))
            {
                SID_NAME_USE snu;
                success = LookupAccountSid(
                    targetComputer,
                    pSid,
                    name,
                    ref nameSize,
                    domainName,
                    ref domainNameSize,
                    out snu
                );
                FreeSid(pSid);
            }

            return success ? name.ToString() : null;
        }
    }


    public static string LookupUserGroupFromRid(string targetComputer, int rid)
    {
        unsafe
        {
            IntPtr umi2;

            StringBuilder domainName = new StringBuilder(bufferSize);
            int domainNameSize = bufferSize;
            StringBuilder name = new StringBuilder(bufferSize);
            int nameSize = bufferSize;

            //
            // get the account domain Sid on the target machine
            // note: if you were looking up multiple sids based on the same
            // account domain, only need to call this once.
            //

            if (NetUserModalsGet(targetComputer, 2, out umi2) != 0)
            {
                throw Win32Kernel.CreateWin32Exception("Error invoking NetUserModalsGet.");
            }

            try
            {
                USER_MODALS_INFO_2 * _umi2 = (USER_MODALS_INFO_2 *) umi2;

                byte SubAuthorityCount = * (byte *) GetSidSubAuthorityCount(_umi2->usrmod2_domain_id);

                //
                // allocate storage for new Sid. account domain Sid + account Rid
                //

                IntPtr pSid = Win32Kernel.HeapAlloc(Win32Kernel.GetProcessHeap(), 0, (UIntPtr)
                GetSidLengthRequired(
                    (byte)
                    (SubAuthorityCount + 1)));

                if (pSid == IntPtr.Zero)
                {
                    throw Win32Kernel.CreateWin32Exception("Error invoking HeapAlloc.");
                }

                try
                {
                    if (!InitializeSid(
                        pSid,
                        GetSidIdentifierAuthority(_umi2->usrmod2_domain_id),
                        (byte)(SubAuthorityCount + 1)
                    ))
                    {
                        throw Win32Kernel.CreateWin32Exception("Error invoking InitializeSid.");
                    }

                    string stringSid;
                    ConvertSidToStringSid(_umi2->usrmod2_domain_id, out stringSid);


                    //
                    // copy existing subauthorities from account domain Sid into
                    // new Sid
                    //

                    for (uint SubAuthIndex = 0; SubAuthIndex < SubAuthorityCount; SubAuthIndex++)
                    {
                        uint * left = (uint *) GetSidSubAuthority(pSid, SubAuthIndex);
                        uint * right = (uint *) GetSidSubAuthority(_umi2->usrmod2_domain_id,
                        SubAuthIndex);
                        * left = * right;
                    }

                    //
                    // append Rid to new Sid
                    //

                    *(uint *) GetSidSubAuthority(pSid, SubAuthorityCount) = (uint) rid;


                    SID_NAME_USE snu;
                    if (!LookupAccountSid(
                        targetComputer,
                        pSid,
                        name,
                        ref nameSize,
                        domainName,
                        ref domainNameSize,
                        out snu))
                    {
                        throw Win32Kernel.CreateWin32Exception("Error invoking LookupAccountSid.");
                    }
                }
                finally
                {
                    Win32Kernel.HeapFree(Win32Kernel.GetProcessHeap(), 0, pSid);
                }
            }
            finally
            {
                NetApiBufferFree(umi2);
            }

            return name.ToString();
        }
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool LookupAccountName(
        string lpSystemName,
        string lpAccountName,
        IntPtr Sid,
        ref int cbSid,
        StringBuilder ReferencedDomainName,
        ref int cchReferencedDomainName,
        out SID_NAME_USE peUse);

    public static string GetWellKnownAccountName(WellKnownSidType sid)
    {
        return ConvertSidToAccountName(new SecurityIdentifier(sid, null).Value);
    }

    public static string ConvertSidToAccountName(string stringSid)
    {
        IntPtr sid;
        if (!ConvertStringSidToSid(stringSid, out sid))
        {
            throw Win32Kernel.CreateWin32Exception("Error invoking ConvertStringSidToSid.");
        }
        try
        {
            StringBuilder domainName = new StringBuilder(bufferSize);
            int domainNameSize = bufferSize;
            StringBuilder userName = new StringBuilder(bufferSize);
            int userNameSize = bufferSize;
            SID_NAME_USE peUse;
            if (
                !LookupAccountSid(null, sid, userName, ref userNameSize, domainName, ref domainNameSize, out peUse))
            {
                throw Win32Kernel.CreateWin32Exception("Error invoking LookupAccountSid.");
            }
            return userName.ToString();
        }
        finally
        {
            Win32Kernel.LocalFree(sid);
        }
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern uint LsaNtStatusToWinError(uint status);

    [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
    private static extern uint LsaAddAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        LSA_UNICODE_STRING[] UserRights,
        uint CountOfRights);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint LsaClose(IntPtr ObjectHandle);

    public static void SetRight(SecurityIdentifier sid, string inPrivilegeName)
    {
        uint aWinErrorCode = 0; //contains the last error
        //pointer an size for the SID
        IntPtr aSid = IntPtr.Zero;
        if (!ConvertStringSidToSid(sid.Value, out aSid))
        {
            throw Win32Kernel.CreateWin32Exception("Error invoking ConvertStringSidToSid.");
        }
        try
        {
            //initialize an empty unicode-string
            LSA_UNICODE_STRING aSystemName = new LSA_UNICODE_STRING();
            //combine all policies
            uint aAccess = (uint)(
                               LSA_AccessPolicy.POLICY_AUDIT_LOG_ADMIN |
                               LSA_AccessPolicy.POLICY_CREATE_ACCOUNT |
                               LSA_AccessPolicy.POLICY_CREATE_PRIVILEGE |
                               LSA_AccessPolicy.POLICY_CREATE_SECRET |
                               LSA_AccessPolicy.POLICY_GET_PRIVATE_INFORMATION |
                               LSA_AccessPolicy.POLICY_LOOKUP_NAMES |
                               LSA_AccessPolicy.POLICY_NOTIFICATION |
                               LSA_AccessPolicy.POLICY_SERVER_ADMIN |
                               LSA_AccessPolicy.POLICY_SET_AUDIT_REQUIREMENTS |
                               LSA_AccessPolicy.POLICY_SET_DEFAULT_QUOTA_LIMITS |
                               LSA_AccessPolicy.POLICY_TRUST_ADMIN |
                               LSA_AccessPolicy.POLICY_VIEW_AUDIT_INFORMATION |
                               LSA_AccessPolicy.POLICY_VIEW_LOCAL_INFORMATION
                           );
            //initialize a pointer for the policy handle
            IntPtr aPolicyHandle = IntPtr.Zero;
            //these attributes are not used, but LsaOpenPolicy wants them to exists
            LSA_OBJECT_ATTRIBUTES aObjectAttributes = new LSA_OBJECT_ATTRIBUTES();
            aObjectAttributes.Length = 0;
            aObjectAttributes.RootDirectory = IntPtr.Zero;
            aObjectAttributes.Attributes = 0;
            aObjectAttributes.SecurityDescriptor = IntPtr.Zero;
            aObjectAttributes.SecurityQualityOfService = IntPtr.Zero;
            //get a policy handle
            uint aOpenPolicyResult = LsaOpenPolicy(ref aSystemName, ref aObjectAttributes, aAccess,
                                                   out aPolicyHandle);
            aWinErrorCode = LsaNtStatusToWinError(aOpenPolicyResult);
            if (aWinErrorCode != 0)
                throw Win32Kernel.CreateWin32Exception((int) aWinErrorCode,
                                                       "Error invoking LsaNtStatusToWinError.");
            try
            {
                //Now that we have the SID an the policy,
                //we can add rights to the account.
                //initialize an unicode-string for the privilege name
                LSA_UNICODE_STRING[] aUserRightsLSAString = new LSA_UNICODE_STRING[1];
                aUserRightsLSAString[0] = new LSA_UNICODE_STRING
                {
                    Buffer = Marshal.StringToHGlobalUni(inPrivilegeName),
                    Length =
                    ((UInt16)
                    (inPrivilegeName.Length * UnicodeEncoding.CharSize)),
                    MaximumLength =
                    ((UInt16)
                    ((inPrivilegeName.Length + 1) * UnicodeEncoding.CharSize))
                };
                //add the right to the account
                uint aLSAResult = LsaAddAccountRights(aPolicyHandle, aSid, aUserRightsLSAString, 1);
                aWinErrorCode = LsaNtStatusToWinError(aLSAResult);
                if (aWinErrorCode != 0)
                    throw Win32Kernel.CreateWin32Exception((int) aWinErrorCode,
                                                           "Error invoking LsaNtStatusToWinError.");
            }
            finally
            {
                LsaClose(aPolicyHandle);
            }
        }
        finally
        {
            Win32Kernel.LocalFree(aSid);
        }
    }

    /// <summary>
    /// Specifies constants that define different ways to log on using
    /// Win32 API <see cref="LogonUser"/>.
    /// </summary>
    public enum LogonType : int
    {
        /// <summary>
        /// This logon type is intended for users who will be interactively
        /// using the computer, such as a user being logged on  by a terminal
        /// server, remote shell, or similar process. This logon type has the
        /// additional expense of caching logon information for disconnected
        /// operations;  therefore, it is inappropriate for some client/server
        /// applications, such as a mail server.
        /// </summary>
        LOGON32_LOGON_INTERACTIVE = 2,

        /// <summary>
        /// This logon type is intended for high performance servers to authenticate
        /// plaintext passwords. The LogonUser function does not cache credentials
        /// for this logon type.
        /// </summary>
        LOGON32_LOGON_NETWORK = 3,

        /// <summary>
        /// This logon type is intended for batch servers, where processes may
        /// be executing on behalf of a user without  their direct intervention.
        /// This type is also for higher performance servers that process many plaintext
        /// authentication attempts at a time, such as mail or Web servers. The
        /// LogonUser function does not cache credentials for this logon type.
        /// </summary>
        LOGON32_LOGON_BATCH = 4,

        /// <summary>
        /// Indicates a service-type logon. The account provided must have the
        /// service privilege enabled.
        /// </summary>
        LOGON32_LOGON_SERVICE = 5,

        /// <summary>
        /// This logon type is for GINA DLLs that log on users who will be
        /// interactively using the computer. This logon type can generate a
        /// unique audit record that shows when the workstation was unlocked.
        /// </summary>
        LOGON32_LOGON_UNLOCK = 7,

        /// <summary>
        /// This logon type preserves the name and password in the authentication
        /// package, which allows the server to make  connections to other network
        /// servers while impersonating the client. A server can accept plaintext
        /// credentials from a client, call LogonUser, verify that the user can
        /// access the system across the network, and still communicate with other
        /// servers.
        /// NOTE: Windows NT:  This value is not supported.
        /// </summary>
        LOGON32_LOGON_NETWORK_CLEARTEXT = 8,

        /// <summary>
        /// This logon type allows the caller to clone its current token and specify
        /// new credentials for outbound connections. The new logon session has the
        /// same local identifier but uses different credentials for other network
        /// connections.
        /// NOTE: This logon type is supported only by the LOGON32_PROVIDER_WINNT50
        /// logon provider.
        /// NOTE: Windows NT:  This value is not supported.
        /// </summary>
        LOGON32_LOGON_NEW_CREDENTIALS = 9,
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool LogonUser(
        string userName,
        string domain,
        string password,
        int logonType,
        int logonProvider,
        out IntPtr phToken
    );

    #region Nested type: ACCESS_MODE

    private enum ACCESS_MODE
    {
        NOT_USED_ACCESS = 0,
        GRANT_ACCESS,
        SET_ACCESS,
        DENY_ACCESS,
        REVOKE_ACCESS,
        SET_AUDIT_SUCCESS,
        SET_AUDIT_FAILURE
    }

    #endregion

    #region Nested type: EXPLICIT_ACCESS

    [StructLayout(LayoutKind.Sequential)]
    private struct EXPLICIT_ACCESS
    {
        public AccessRights grfAccessPermissions;
        public ACCESS_MODE grfAccessMode;
        public int grfInheritance;
        public TRUSTEE Trustee;
    }

    #endregion

    #region Nested type: GenericRightMapping

    [StructLayout(LayoutKind.Sequential)]
    public struct GenericRightMapping
    {
        public AccessRights GenericRead;
        public AccessRights GenericWrite;
        public AccessRights GenericExecute;
        public AccessRights GenericAll;
    }

    #endregion

    #region Nested type: LSA_OBJECT_ATTRIBUTES

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_OBJECT_ATTRIBUTES
    {
        public UInt32 Length;
        public IntPtr RootDirectory;
        public LSA_UNICODE_STRING ObjectName;
        public UInt32 Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    #endregion

    #region Nested type: LSA_UNICODE_STRING

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_UNICODE_STRING
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public IntPtr Buffer;
    }

    #endregion

    #region Nested type: LUID

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public UInt32 LowPart;
        public Int32 HighPart;
    }

    #endregion

    #region Nested type: LUID_AND_ATTRIBUTES

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public PRIVILEGE_SET_ATTRIBUTES Attributes;
    }

    #endregion

    #region Nested type: MULTIPLE_TRUSTEE_OPERATION

    private enum MULTIPLE_TRUSTEE_OPERATION
    {
        NO_MULTIPLE_TRUSTEE,
        TRUSTEE_IS_IMPERSONATE
    }

    #endregion

    #region Nested type: PRIVILEGE_SET

    [StructLayout(LayoutKind.Sequential)]
    private struct PRIVILEGE_SET
    {
        public uint PrivilegeCount;
        public PRIVILEGE_SET_CONTROL Control;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)] public LUID_AND_ATTRIBUTES[] Privilege;
    }

    #endregion

    #region Nested type: PRIVILEGE_SET_ATTRIBUTES

    private enum PRIVILEGE_SET_ATTRIBUTES : uint
    {
        SE_PRIVILEGE_NONE = 0,
        SE_PRIVILEGE_ENABLED_BY_DEFAULT = (0x00000001),
        SE_PRIVILEGE_ENABLED = (0x00000002),
        SE_PRIVILEGE_REMOVED = (0X00000004),
        SE_PRIVILEGE_USED_FOR_ACCESS = (0x80000000),
        SE_PRIVILEGE_VALID_ATTRIBUTES = (SE_PRIVILEGE_ENABLED_BY_DEFAULT |
        SE_PRIVILEGE_ENABLED |
        SE_PRIVILEGE_REMOVED |
        SE_PRIVILEGE_USED_FOR_ACCESS)
    }

    #endregion

    #region Nested type: PRIVILEGE_SET_CONTROL

    private enum PRIVILEGE_SET_CONTROL : uint
    {
        PRIVILEGE_SET_ALL_NECESSARY = 1
    }

    #endregion

    #region Nested type: Privileges

    public static class Privileges
    {
        public const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
        public const string SE_AUDIT_NAME = "SeAuditPrivilege";
        public const string SE_BACKUP_NAME = "SeBackupPrivilege";
        public const string SE_BATCH_LOGON_NAME = "SeBatchLogonRight";
        public const string SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";
        public const string SE_CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";
        public const string SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";
        public const string SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";
        public const string SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
        public const string SE_DEBUG_NAME = "SeDebugPrivilege";
        public const string SE_DENY_BATCH_LOGON_NAME = "SeDenyBatchLogonRight";
        public const string SE_DENY_INTERACTIVE_LOGON_NAME = "SeDenyInteractiveLogonRight";
        public const string SE_DENY_NETWORK_LOGON_NAME = "SeDenyNetworkLogonRight";
        public const string SE_DENY_REMOTE_INTERACTIVE_LOGON_NAME = "SeDenyRemoteInteractiveLogonRight";
        public const string SE_DENY_SERVICE_LOGON_NAME = "SeDenyServiceLogonRight";
        public const string SE_ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";
        public const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";
        public const string SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
        public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        public const string SE_INTERACTIVE_LOGON_NAME = "SeInteractiveLogonRight";
        public const string SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";
        public const string SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";
        public const string SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";
        public const string SE_MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";
        public const string SE_NETWORK_LOGON_NAME = "SeNetworkLogonRight";
        public const string SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        public const string SE_REMOTE_INTERACTIVE_LOGON_NAME = "SeRemoteInteractiveLogonRight";
        public const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";
        public const string SE_RESTORE_NAME = "SeRestorePrivilege";
        public const string SE_SECURITY_NAME = "SeSecurityPrivilege";
        public const string SE_SERVICE_LOGON_NAME = "SeServiceLogonRight";
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        public const string SE_SYNC_AGENT_NAME = "SeSyncAgentPrivilege";
        public const string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";
        public const string SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";
        public const string SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";
        public const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
        public const string SE_TCB_NAME = "SeTcbPrivilege";
        public const string SE_UNDOCK_NAME = "SeUndockPrivilege";
        public const string SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";
    }

    #endregion

    #region Nested type: SECURITY_IMPERSONATION_LEVEL

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous
        ,
        SecurityIdentification
        ,
        SecurityImpersonation
        ,
        SecurityDelegation
    }

    #endregion

    #region Nested type: SECURITY_INFORMATION

    internal enum SECURITY_INFORMATION : uint
    {
        OWNER_SECURITY_INFORMATION = 0x00000001,
        GROUP_SECURITY_INFORMATION = 0x00000002,
        DACL_SECURITY_INFORMATION = 0x00000004,
        SACL_SECURITY_INFORMATION = 0x00000008,
        LABEL_SECURITY_INFORMATION = 0x00000010,

        PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000,
        PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
        UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
        UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
    }

    #endregion

    #region Nested type: SID_IDENTIFIER_AUTHORITY

    public struct SID_IDENTIFIER_AUTHORITY
    {
#pragma warning disable 649
        private unsafe fixed byte value [6];
#pragma warning restore 649

        public SID_IDENTIFIER_AUTHORITY(byte[] value)
        {
            unsafe
            {
                fixed (byte * p = this.value)
                    Marshal.Copy(value, 0, new IntPtr(p), 6);
            }
        }
    }

    #endregion

    #region Nested type: TOKEN_PRIVILEGES

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public static readonly int SizeOf = 4 + IntPtr.Size;

        public UInt32 PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)] public LUID_AND_ATTRIBUTES[] Privileges;

        public const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
        public const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;
    }

    #endregion

    #region Nested type: TRUSTEE

    [StructLayout(LayoutKind.Sequential)]
    private struct TRUSTEE
    {
        public IntPtr pMultipleTrustee;
        public MULTIPLE_TRUSTEE_OPERATION MultipleTrusteeOperation;
        public TRUSTEE_FORM TrusteeForm;
        public TRUSTEE_TYPE TrusteeType;
        public IntPtr ptstrName;
    } ;

    #endregion

    #region Nested type: TRUSTEE_FORM

    private enum TRUSTEE_FORM
    {
        TRUSTEE_IS_SID,
        TRUSTEE_IS_NAME,
        TRUSTEE_BAD_FORM,
        TRUSTEE_IS_OBJECTS_AND_SID,
        TRUSTEE_IS_OBJECTS_AND_NAME,
    }

    #endregion

    #region Nested type: TRUSTEE_TYPE

    private enum TRUSTEE_TYPE
    {
        TRUSTEE_IS_UNKNOWN
        ,
        TRUSTEE_IS_USER
        ,
        TRUSTEE_IS_GROUP
        ,
        TRUSTEE_IS_DOMAIN
        ,
        TRUSTEE_IS_ALIAS
        ,
        TRUSTEE_IS_WELL_KNOWN_GROUP
        ,
        TRUSTEE_IS_DELETED
        ,
        TRUSTEE_IS_INVALID
        ,
        TRUSTEE_IS_COMPUTER
    }

    #endregion

    #region Nested type: USER_MODALS_INFO_2

    [StructLayout(LayoutKind.Sequential)]
    private struct USER_MODALS_INFO_2
    {
        public IntPtr usrmod2_domain_name;
        public IntPtr usrmod2_domain_id;
    }

    #endregion

    #region Nested type: WellKnownRid

    public static class WellKnownRid
    {
        // well-known aliases ...

        public const int DOMAIN_ALIAS_RID_ACCOUNT_OPS = 0x00000224;
        public const int DOMAIN_ALIAS_RID_ADMINS = 0x00000220;
        public const int DOMAIN_ALIAS_RID_BACKUP_OPS = 0x00000227;
        public const int DOMAIN_ALIAS_RID_GUESTS = 0x00000222;
        public const int DOMAIN_ALIAS_RID_POWER_USERS = 0x00000223;
        public const int DOMAIN_ALIAS_RID_PRINT_OPS = 0x00000226;
        public const int DOMAIN_ALIAS_RID_REPLICATOR = 0x00000228;
        public const int DOMAIN_ALIAS_RID_SYSTEM_OPS = 0x00000225;
        public const int DOMAIN_ALIAS_RID_USERS = 0x00000221;
        public const int DOMAIN_GROUP_RID_ADMINS = 0x00000200;
        public const int DOMAIN_GROUP_RID_GUESTS = 0x00000202;
        public const int DOMAIN_GROUP_RID_USERS = 0x00000201;
        public const int DOMAIN_USER_RID_ADMIN = 0x000001F4;
        public const int DOMAIN_USER_RID_GUEST = 0x000001F5;
        public const int SECURITY_ANONYMOUS_LOGON_RID = 0x00000007;
        public const int SECURITY_BATCH_RID = 0x00000003;
        public const int SECURITY_BUILTIN_DOMAIN_RID = 0x00000020;

        public const int SECURITY_CREATOR_GROUP_RID = 0x00000001;

        public const int SECURITY_CREATOR_GROUP_SERVER_RID = 0x00000003;
        public const int SECURITY_CREATOR_OWNER_RID = 0x00000000;
        public const int SECURITY_CREATOR_OWNER_SERVER_RID = 0x00000002;


        public const int SECURITY_DIALUP_RID = 0x00000001;
        public const int SECURITY_INTERACTIVE_RID = 0x00000004;
        public const int SECURITY_LOCAL_RID = 0X00000000;
        public const int SECURITY_LOCAL_SYSTEM_RID = 0x00000012;

        public const int SECURITY_LOGON_IDS_RID = 0x00000005;
        public const int SECURITY_LOGON_IDS_RID_COUNT = 3;
        public const int SECURITY_NETWORK_RID = 0x00000002;

        public const int SECURITY_NT_NON_UNIQUE = 0x00000015;
        public const int SECURITY_NULL_RID = 0x00000000;
        public const int SECURITY_PROXY_RID = 0x00000008;
        public const int SECURITY_SERVER_LOGON_RID = 0x00000009;
        public const int SECURITY_SERVICE_RID = 0x00000006;
        public const int SECURITY_WORLD_RID = 0x00000000;
    }

    #endregion

    #region Nested type: WellKnownSid

    public static class WellKnownSid
    {
        //       The following section is for informational purposes and is useful
        //       for visualizing Sid values:

        // Universal well-known SIDs:
        //
        //     Null SID                     S-1-0-0
        //     World                        S-1-1-0
        //     Local                        S-1-2-0
        //     Creator Owner ID             S-1-3-0
        //     Creator Group ID             S-1-3-1
        //     Creator Owner Server ID      S-1-3-2
        //     Creator Group Server ID      S-1-3-3
        //
        //     (Non-unique IDs)             S-1-4
        //
        // NT well-known SIDs:
        //
        //     NT Authority          S-1-5
        //     Dialup                S-1-5-1
        //
        //     Network               S-1-5-2
        //     Batch                 S-1-5-3
        //     Interactive           S-1-5-4
        //     Service               S-1-5-6
        //     AnonymousLogon        S-1-5-7       (aka null logon session)
        //     Proxy                 S-1-5-8
        //     ServerLogon           S-1-5-8       (aka domain controller
        //                                            account)
        //
        //     (Logon IDs)           S-1-5-5-X-Y
        //
        //     (NT non-unique IDs)   S-1-5-0x15-...
        //
        //     (Built-in domain)     S-1-5-0x20

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_CREATOR_SID_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 3});

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_LOCAL_SID_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 2});

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_NON_UNIQUE_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 4});

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_NT_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 5});   // ntifs

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_NULL_SID_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 0});

        public static readonly SID_IDENTIFIER_AUTHORITY SECURITY_WORLD_SID_AUTHORITY =
            new SID_IDENTIFIER_AUTHORITY(new byte[] {0, 0, 0, 0, 0, 1});
    }

    #endregion
}
}