
using Starcounter.Internal;
using Starcounter.Server.Service;
using Starcounter.Server.Setup;
using Starcounter.Server.Windows;
using System;

namespace staradmin {

    internal static class SystemServiceInstall {

        internal static void Install() {
            PreInstall();
            var setup = new SystemServiceSetup();
            setup.StartupType = StartupType.Manual;
            setup.Execute();
        }

        internal static void Uninstall() {
            SystemServerService.Delete(SystemServerService.Name);
            PostUnInstall();
        }

        static void PreInstall() {
            var installDir = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            if (string.IsNullOrEmpty(installDir)) {
                throw new InvalidOperationException();
            }
            Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, installDir, EnvironmentVariableTarget.Machine);
        }

        static void PostUnInstall() {
            var x = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(x)) {
                Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, null, EnvironmentVariableTarget.Machine);
            }
        }
    }
}
