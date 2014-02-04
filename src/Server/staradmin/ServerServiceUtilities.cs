
using Starcounter.Internal;
using Starcounter.Server.Service;
using Starcounter.Server.Setup;
using Starcounter.Server.Windows;
using System;
using System.ServiceProcess;

namespace staradmin {

    internal static class ServerServiceUtilities {

        internal static void Install(bool forceStart = false) {
            PreInstall();
            var setup = new ServerServiceSetup();
            setup.StartupType = StartupType.Manual;
            setup.Execute();
            PostInstall(setup.ServiceName, forceStart ? StartupType.Automatic : setup.StartupType);
        }

        internal static void Uninstall() {
            var name = ServerService.Name;
            PreUnInstall(name);
            ServerService.Delete(name);
            PostUnInstall();
        }

        internal static void Start(string serviceName = ServerService.Name) {
            ServerService.Start(serviceName);
        }

        internal static void Stop(string serviceName = ServerService.Name) {
            ServerService.Stop(serviceName);
        }

        static void PreInstall() {
            var installDir = StarcounterEnvironment.InstallationDirectory;
            if (string.IsNullOrEmpty(installDir)) {
                throw new InvalidOperationException();
            }
            Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, installDir, EnvironmentVariableTarget.Machine);
        }

        static void PostInstall(string serviceName, StartupType startupType) {
            if (startupType == StartupType.Automatic) {
                Start();
            }
        }

        static void PreUnInstall(string serviceName) {
            Stop(serviceName);
        }

        static void PostUnInstall() {
            var x = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(x)) {
                Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, null, EnvironmentVariableTarget.Machine);
            }
        }
    }
}
