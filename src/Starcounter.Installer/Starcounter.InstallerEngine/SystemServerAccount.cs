
using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Starcounter.Advanced.Configuration;
using Starcounter.Management.Win32;
using System.DirectoryServices;
using System.Collections;
using System.Configuration.Install;
using System.Security;
using Starcounter.Internal;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Starcounter.InstallerEngine
{
/// <summary>
/// Exposes functionality that allows clients to retrieve the account
/// information for the Starcounter system server account, such as it's
/// name and it's password, and provides methods to create and configure
/// the account if needed.
/// </summary>
public static class SystemServerAccount
{
    /// <summary>
    /// The default name used for the system server user account when
    /// created.
    /// </summary>
    public const string UserName = "ScSystemUser";

    /// <summary>
    /// The default name of the system server user group when created.
    /// </summary>
    public const string GroupName = "StarcounterUsers";

    /// <summary>
    /// The registry key we use to store account information to be able
    /// to retrieve it when needed.
    /// </summary>
    private static string AccountKey;

    /// <summary>
    /// Initializes process wide information.
    /// </summary>
    static SystemServerAccount()
    {
        SystemServerAccount.AccountKey = ((IntPtr.Size == 8) ? @"SOFTWARE\Starcounter\Account"
                                                             : @"SOFTWARE\Wow6432Node\Starcounter\Account");
    }

    /// <summary>
    /// Changes the account key corresponding to installation platform.
    /// </summary>
    /// <param name="is64Bit">Is 64-bit installation?</param>
    public static void ChangeInstallationPlatform(Boolean is64Bit)
    {
        SystemServerAccount.AccountKey = (is64Bit ? @"SOFTWARE\Starcounter\Account"
                                                  : @"SOFTWARE\Wow6432Node\Starcounter\Account");
    }

    /// <summary>
    /// Random alphabet used to generate strong passwords.
    /// </summary>
    private static readonly char[] randomAlphabet =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    /// <summary>
    /// Recursively adds specified access rights to a directory.
    /// </summary>
    /// <param name="fsi">Current file system information (directory or file)</param>
    /// <param name="directoryAccessRule"></param>
    /// <param name="fileAccessRule"></param>
    public static void AddDirectoryAccessRights(FileSystemInfo fsi,
                                                FileSystemAccessRule directoryAccessRule,
                                                FileSystemAccessRule fileAccessRule)
    {
        // Checking for dead end.
        if (fsi == null) return;

        // Checking if its a directory and go into recursion if yes.
        var dirInfo = fsi as DirectoryInfo;
        if (dirInfo != null) // Its a directory.
        {
            try
            {
                // Checking if directory exists.
                if (!Directory.Exists(fsi.FullName)) return;

                // Getting existing access control rules for this directory.
                DirectorySecurity security = Directory.GetAccessControl(fsi.FullName);

                // Adding needed security access rule.
                security.AddAccessRule(directoryAccessRule);

                // Applying security changes.
                Directory.SetAccessControl(fsi.FullName, security);
            }
            catch
            {
                throw new SecurityException("INTERNAL ERROR: Problem assigning rights for Starcounter user for the directory:\n" +
                                            fsi.FullName +
                                            "\nPlease choose another folder for binaries and system server upon next installation attempt." +
                                            "\nThank you.");
            }

            // Trying to obtain sub-information for the directory.
            FileSystemInfo[] fsis = dirInfo.GetFileSystemInfos();

            // Iterating through each sub-folder.
            if (fsis != null)
            {
                foreach (var subDirInfo in fsis)
                {
                    // Go into recursion for each sub-directory/file.
                    AddDirectoryAccessRights(subDirInfo,
                                             directoryAccessRule,
                                             fileAccessRule);
                }
            }
        }
        else // Its a file.
        {
            try
            {
                // Checking if file exists.
                if (!File.Exists(fsi.FullName)) return;

                // Getting existing access control rules for this file.
                FileSecurity security = File.GetAccessControl(fsi.FullName);

                // Adding needed security access rule.
                security.AddAccessRule(fileAccessRule);

                // Applying security changes.
                File.SetAccessControl(fsi.FullName, security);
            }
            catch
            {
                throw new SecurityException("INTERNAL ERROR: Problem assigning rights for Starcounter user for the file:\n" +
                                            fsi.FullName +
                                            "\nPlease choose another folder for binaries and system server upon next installation attempt." +
                                            "\nThank you.");
            }
        }
    }

    /// <summary>
    /// Gets the account information for the system server account used
    /// when running Starcounter servers and databases under Windows
    /// service management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method can only be invoked by users with administration
    /// rights on the machine on which it executes or an exception will
    /// be raised.
    /// </para>
    /// <para>
    /// If the information can not be found in the place where Starcounter
    /// stores such information, this method tries creating a new account
    /// with default settings.
    /// </para>
    /// </remarks>
    /// <param name="name">
    /// Name of the Starcounter system server account.</param>
    /// <param name="password">
    /// Password for the Starcounter system server account.</param>
    /// <returns>True if the information returned was retrieved from a previously
    /// created account. False if the account was created.</returns>
    /// <param name="binariesPath"></param>
    /// <param name="systemServerPath"></param>
    public static bool AssureAccount(String binariesPath, String systemServerPath, out string name, out string password)
    {
        PrincipalContext context;
        UserPrincipal user;

        using(RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(SystemServerAccount.AccountKey, false))
        {
            // The account information was not in the registry. We interpret this
            // as if we are executing on a non-prepared machine and we must
            // create it, assuming the account does not exist.
            name = SystemServerAccount.UserName;
            context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
            user = UserPrincipal.FindByIdentity(context, UserName);
            
            if (registryKey != null)
            {
                // We have preserved the account information. Retrieve it and
                // return it, and return the indication that this was the case.
                name = (string) registryKey.GetValue("Name");
                password = (string) registryKey.GetValue("Password");
                
                // Checking if user exist, and return success if yes.
                if (user != null) return true;
                
                // Otherwise we through an exception.
                throw new InstallException("Starcounter registry entry exists but not the Starcounter user. Please re-install Starcounter.");
            }

            if (user != null)
            {
                // The account was already there, but we have not the updated
                // information about it in the registry. We can't fetch the password
                // and it would be inappropriate to just reset it (since it can be
                // used by configured servers). Conclusion: we can't return anything
                // that can really be used.
                
                throw new InstallException("Starcounter user account exists but not the Starcounter registry entry. Please re-install Starcounter.");
            }

            // The account does not exist; we can go on and create it.
            // As suggested by Windows, we create a user account whose password
            // never expires, since it is intended to be used as a service
            // logon account.
            
            Trace.TraceInformation("Starcounter user/password pair not found. Creating it.");
            password = GenerateNewPassword();
            user = new UserPrincipal(context, name, password, true)
            {
                DisplayName = "Starcounter service user account",
                Description = "Account used by Starcounter when running servers and databases as daemon system level processes.",
                PasswordNeverExpires = true
            };
            user.Save();

            // Adding needed system privileges to the created user.
            Win32Security.SetRight(user.Sid, Win32Security.Privileges.SE_MANAGE_VOLUME_NAME);
            Win32Security.SetRight(user.Sid, Win32Security.Privileges.SE_SERVICE_LOGON_NAME);
            Win32Security.SetRight(user.Sid, Win32Security.Privileges.SE_ASSIGNPRIMARYTOKEN_NAME);
            Win32Security.SetRight(user.Sid, Win32Security.Privileges.SE_CREATE_GLOBAL_NAME);

            // Setting the privilege to debug programs (needed only in shared memory monitor ScConnMonitor).
            Win32Security.SetRight(user.Sid, Win32Security.Privileges.SE_DEBUG_NAME);

            // Directories that need rights to be assigned to.
            String[] accessDirs = { binariesPath,
                systemServerPath,
                StarcounterEnvironment.Directories.SystemAppDataDirectory };

            // Adding rights to the binaries and server folder.
            foreach (String accessDir in accessDirs)
            {
                if (accessDir == null)
                    continue;

                // Checking if directory exists, otherwise creating it.
                if (!Directory.Exists(accessDir))
                    Directory.CreateDirectory(accessDir);

                // Adding access rule for Starcounter user.
                AddDirectoryAccessRights(new DirectoryInfo(accessDir),

                                         // Directories access rights.
                                         new FileSystemAccessRule(user.Sid, FileSystemRights.Modify,
                                                                  InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                                  PropagationFlags.None,
                                                                  AccessControlType.Allow),

                                         // Files access rights.
                                         new FileSystemAccessRule(user.Sid, FileSystemRights.Modify,
                                                                  InheritanceFlags.None,
                                                                  PropagationFlags.None,
                                                                  AccessControlType.Allow));
            }

            // Store the account information in the registry, restricted to
            // by used by local administrators only.
            StoreAccountInformation(name, password);
            return false;
        }
    }

    // Creates command line for service executable.
    private static string MakeCommandLine(String binPath)
    {
        StringBuilder commandLine;
        string executable;
        string arguments;
        commandLine = new StringBuilder();
        executable = StarcounterConstants.ProgramNames.ScService + ".exe";
        arguments = " System";
        commandLine = new StringBuilder(binPath.Length + executable.Length + arguments.Length + 128);

        commandLine.Append("\"" + Path.Combine(binPath, executable) + "\"");
        commandLine.Append(" ");
        commandLine.Append(arguments);

        return commandLine.ToString();
    }

    // Should be called after assure account.
    public static void CreateService(String binariesPath, String serviceName, String user, String password)
    {
        IntPtr serviceManagerHandle = Win32Service.OpenSCManager(null, null, (uint)Win32Service.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG);
        if (serviceManagerHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            // Creating service command line in specified binaries folder.
            String commandLine = MakeCommandLine(binariesPath);

            SystemServerService.Create(
                serviceManagerHandle,
                "Starcounter Service",
                serviceName,
                "Starcounter Service",
                StartupType.Automatic,
                commandLine,
                user,
                password
                );
        }
        finally
        {
            Win32Service.CloseServiceHandle(serviceManagerHandle);
        }
    }

    /// <summary>
    /// Generates a strong password to be used when
    /// </summary>
    /// <returns></returns>
    private static string GenerateNewPassword()
    {
        RandomNumberGenerator random;
        const int passwordSize = 64;
        byte[] randomBytes;
        StringBuilder passwordBuilder;
        random = RandomNumberGenerator.Create();
        randomBytes = new byte[passwordSize];
        random.GetBytes(randomBytes);
        passwordBuilder = new StringBuilder(passwordSize);
        for (int i = 0; i < passwordSize; i++)
        {
            passwordBuilder.Append(randomAlphabet[randomBytes[i] % randomAlphabet.Length]);
        }
        return passwordBuilder.ToString();
    }

    /// <summary>
    /// Stores the given information in the registry in the well-known
    /// Starcounter account hive, securing it from every other user except
    /// local administrators.
    /// </summary>
    /// <param name="name">The account name.</param>
    /// <param name="password">The password.</param>
    private static void StoreAccountInformation(string name, string password)
    {
        RegistryKey key;
        RegistrySecurity keySecurity;
        key = Registry.LocalMachine.CreateSubKey(SystemServerAccount.AccountKey);
        key.SetValue("Name", name, RegistryValueKind.String);
        key.SetValue("Password", password, RegistryValueKind.String);
        keySecurity = key.GetAccessControl(AccessControlSections.Access);
        keySecurity.SetAccessRuleProtection(true, false);
        SecurityIdentifier adminIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

        keySecurity.SetAccessRule(
            new RegistryAccessRule(adminIdentifier,
                                   RegistryRights.FullControl,
                                   InheritanceFlags.ContainerInherit,
                                   PropagationFlags.None,
                                   AccessControlType.Allow)
        );

        // TODO: Caused security problems on execution. Needs resolution.
        /*
        SecurityIdentifier everyoneIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        keySecurity.AddAuditRule(
            new RegistryAuditRule(everyoneIdentifier,
                                  RegistryRights.FullControl,
                                  InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                  PropagationFlags.None,
                                  AuditFlags.Success | AuditFlags.Failure)
        );
        */

        key.SetAccessControl(keySecurity);
    }
}
}