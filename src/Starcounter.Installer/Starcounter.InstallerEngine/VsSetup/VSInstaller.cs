
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter;

namespace Starcounter.InstallerEngine.VsSetup
{
    static class VSInstaller
    {
        public static void InstallVs2010(string binDirectory)
        {
            InstallUsingVSIXInstaller(
                Path.Combine(ConstantsBank.VS2010IDEDirectory, ConstantsBank.VSIXInstallerEngineExecutable),
                Path.Combine(binDirectory, VSIXPackageInfo.VS2010.FileName),
                "10.0",
                "Pro"
                );
        }

        public static void UninstallVs2010(string binDirectory)
        {
            UninstallUsingVSIXInstaller(
                Path.Combine(ConstantsBank.VS2010IDEDirectory, ConstantsBank.VSIXInstallerEngineExecutable),
                VSIXPackageInfo.VS2010.ExtensionIdentity,
                "10.0",
                "Pro"
                );
        }

        public static void InstallVs2012(string binDirectory)
        {
            InstallUsingVSIXInstaller(
                Path.Combine(ConstantsBank.VS2012IDEDirectory, ConstantsBank.VSIXInstallerEngineExecutable),
                Path.Combine(binDirectory, VSIXPackageInfo.VS2012.FileName),
                "11.0",
                "Pro"
                );
        }

        public static void UninstallVs2012(string binDirectory)
        {
            UninstallUsingVSIXInstaller(
                Path.Combine(ConstantsBank.VS2012IDEDirectory, ConstantsBank.VSIXInstallerEngineExecutable),
                VSIXPackageInfo.VS2012.ExtensionIdentity,
                "11.0",
                "Pro"
                );
        }

        /// <summary>
        /// Installs a VSIX package using the Visual Studio VSIX installer engine.
        /// </summary>
        /// <param name="installerEnginePath">
        /// Path to the engine to use.</param>
        /// <param name="vsixPackageFile">
        /// Path to the VSIX package to install.</param>
        /// <param name="targetEdition">
        /// The version (build number) of Visual Studio to install into, e.g. "11.0"
        /// for Visual Studio 2012.</param>
        /// <param name="targetVersion">
        /// The edition of Visual Studio to install into, normally "Pro" (for
        /// "Professional").
        /// </param>
        static void InstallUsingVSIXInstaller(
            string installerEnginePath,
            string vsixPackageFile, 
            string targetVersion,
            string targetEdition 
            )
        {
            Process process;
            string arguments;
            int timeoutInMilliseconds;
            int result;
            bool completed;

            if (!File.Exists(installerEnginePath))
            {
                throw ErrorCode.ToException(
                    Error.SCERRVSIXENGINENOTFOUND,
                    string.Format("Engine path={0}, Target version={1}, Target edition={2}",
                    installerEnginePath,
                    targetVersion,
                    targetEdition));
            }

            if (!File.Exists(vsixPackageFile))
            {
                throw ErrorCode.ToException(
                    Error.SCERRVSIXPACKAGENOTFOUND,
                    string.Format("Package path={0}, Target version={1}, Target edition={2}",
                    vsixPackageFile,
                    targetVersion,
                    targetEdition));
            }

            // Example command-line:
            // VSIXInstaller.exe /quiet /skuName:Pro /skuVersion:11.0 "C:\Program Files\Starcounter\Starcounter.VisualStudio.11.0.vsix"
            
            arguments = string.Format(
                "/quiet /skuName:{0} /skuVersion:{1} \"{2}\"",
                targetEdition,
                targetVersion,
                vsixPackageFile
                );

            try
            {
                process = Process.Start(installerEnginePath, arguments);
            }
            catch (Exception startupException)
            {
                throw ErrorCode.ToException(Error.SCERRVSIXENGINECOULDNOTSTART,
                    startupException,
                    string.Format(
                    "Exception message={0}, Engine path={1}, Arguments={2}",
                    startupException.Message,
                    installerEnginePath,
                    arguments)
                    );
            }

            timeoutInMilliseconds = 120 * 1000;
            completed = process.WaitForExit(timeoutInMilliseconds);            
            if (!completed)
            {
                throw ErrorCode.ToException(Error.SCERRVSIXENGINETIMEDOUT,
                    string.Format(
                    "Timeout time={0} ms, Engine path={1}, Arguments={2}",
                    timeoutInMilliseconds,
                    installerEnginePath,
                    arguments)
                    );
            }

            result = process.ExitCode;
            if (result != 0)
            {
                throw ErrorCode.ToException(Error.SCERRVSIXENGINEFAILED,
                    string.Format(
                    "Process exit code={0}, Engine path={1}, Arguments={2}",
                    result,
                    installerEnginePath,
                    arguments)
                    );
            }
        }

        static void UninstallUsingVSIXInstaller(
            string installerEnginePath,
            string vsixExtensionIdentity,
            string targetVersion,
            string targetEdition)
        {
            Process process;
            string arguments;
            int timeoutInMilliseconds;
            int result;
            bool completed;

            if (!File.Exists(installerEnginePath))
            {
                throw ErrorCode.ToException(
                    Error.SCERRVSIXENGINENOTFOUND,
                    string.Format("Engine path={0}, Target version={1}, Target edition={2}",
                    installerEnginePath,
                    targetVersion,
                    targetEdition));
            }

            // Example command-line:
            // VSIXInstaller.exe /quiet /skuName:Pro /skuVersion:11.0 /uninstall:Starcounter.VS11.Extension.c3ff293a-4564-4f91-8fb8-635a64f6b310

            arguments = string.Format(
                "/quiet /skuName:{0} /skuVersion:{1} /uninstall:{2}",
                targetEdition,
                targetVersion,
                vsixExtensionIdentity
                );

            try
            {
                process = Process.Start(installerEnginePath, arguments);
            }
            catch (Exception startupException)
            {
                throw ErrorCode.ToException(Error.SCERRVSIXENGINECOULDNOTSTART,
                    startupException,
                    string.Format(
                    "Exception message={0}, Engine path={1}, Arguments={2}",
                    startupException.Message,
                    installerEnginePath,
                    arguments)
                    );
            }

            timeoutInMilliseconds = 120 * 1000;
            completed = process.WaitForExit(timeoutInMilliseconds);
            if (!completed)
            {
                throw ErrorCode.ToException(Error.SCERRVSIXENGINETIMEDOUT,
                    string.Format(
                    "Timeout time={0} ms, Engine path={1}, Arguments={2}",
                    timeoutInMilliseconds,
                    installerEnginePath,
                    arguments)
                    );
            }

            result = process.ExitCode;
            if (result != 0)
            {
                if (UninstallEngine.CompleteCleanupSetting && result == 2003)
                {
                    // 2003 is the exit code indicating that a package was not installed.
                    // If we execute in the context of a full cleanup, we have no certain
                    // state (i.e. the installation can be corrupt) and so we let this
                    // one slide without reporting an exception.

                    Utilities.LogMessage(
                        ErrorCode.ToMessage(Error.SCERRVSIXENGINEFAILED,
                        string.Format(
                        "The result was ignored due to the uninstaller indicating we are doing a full cleanup. Process exit code={0}, Engine path={1}, Arguments={2}",
                        result,
                        installerEnginePath,
                        arguments)));
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRVSIXENGINEFAILED,
                        string.Format(
                        "Process exit code={0}, Engine path={1}, Arguments={2}",
                        result,
                        installerEnginePath,
                        arguments)
                        );
                }
            }
        }
    }
}