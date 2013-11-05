
using Starcounter.Internal;
using Starcounter.Server.Service;
using Starcounter.Server.Setup;
using Starcounter.Server.Windows;
using System;
using System.ServiceProcess;

namespace staradmin {

    internal static class SystemServiceInstall {

        internal static void Install(bool forceStart = false) {
            PreInstall();
            var setup = new SystemServiceSetup();
            setup.StartupType = StartupType.Manual;
            setup.Execute();
            PostInstall(setup.ServiceName, forceStart ? StartupType.Automatic : setup.StartupType);
        }

        internal static void Uninstall() {
            var name = SystemServerService.Name;
            PreUnInstall(name);
            SystemServerService.Delete(name);
            PostUnInstall();
        }

        static void PreInstall() {
            var installDir = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            if (string.IsNullOrEmpty(installDir)) {
                throw new InvalidOperationException();
            }
            Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, installDir, EnvironmentVariableTarget.Machine);
        }

        static void PostInstall(string serviceName, StartupType startupType) {
            if (startupType == StartupType.Automatic) {
                var controller = new ServiceController(serviceName);
                try {
                    controller.Start();
                } finally {
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
        }

        static void PreUnInstall(string serviceName) {
            var controller = new ServiceController(serviceName);
            if (controller.Status != ServiceControllerStatus.Stopped) {
                try {
                    controller.Stop();
                } finally {
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }
        }

        static void PostUnInstall() {
            var x = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(x)) {
                Environment.SetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory, null, EnvironmentVariableTarget.Machine);
            }
        }
    }
}
