
using Starcounter;
using System;
using System.Diagnostics;
using System.IO;

namespace Starcounter.InstallerEngine.VsSetup
{  
    /// <summary>
    /// Encapsulates an instance of devenv.exe for a certain
    /// Visual Studio version.
    /// </summary>
    internal class DevEnv
    {
        /// <summary>
        /// Path to the devenv.exe executable.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The version of Visual Studio whose devenv.exe this instance
        /// encapsulates.
        /// </summary>
        public readonly VisualStudioVersion Version;

        public DevEnv(VisualStudioVersion version)
        {
            this.Version = version;
            this.FilePath = Path.GetFullPath(ConstantsBank.VS2012DevEnvPath);
        }

        internal void InstallTemplates(bool developmentInstallation)
        {
            string arguments;

            arguments = MakeDevEnvArguments(
                true,
                false,
                developmentInstallation,
                developmentInstallation
                );

            RunProgram(arguments);
        }

        private string MakeDevEnvArguments(
            bool installVsTemplates,
            bool setup,
            bool inExperimentalHive,
            bool underRanu
            )
        {
            string arguments;

            arguments = string.Format("{0} {1} {2} {3}",
                installVsTemplates ? "/InstallVSTemplates" : string.Empty,
                setup ? "/setup" : string.Empty,
                inExperimentalHive ? "/rootSuffix Exp" : string.Empty,
                underRanu ? "/RANU" : string.Empty
                );

            return arguments.Trim();
        }

        private void RunProgram(string arguments)
        {
            Process proc;

            proc = Process.Start(this.FilePath, arguments);
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw ErrorCode.ToException(
                    Error.SCERRINSTALLERPROCESSWRONGERRORCODE,
                    string.Format("[{0}]", FormatExitMessage(proc))
                    );
            }
        }

        static string FormatExitMessage(Process toolProcess)
        {
            if (toolProcess == null)
                throw new ArgumentNullException("toolProcess");

            return string.Format("\"\"{0}\" {1}\" => Exit code: {2}",
                toolProcess.StartInfo.FileName,
                toolProcess.StartInfo.Arguments,
                toolProcess.ExitCode
                );
        }
    }
}
