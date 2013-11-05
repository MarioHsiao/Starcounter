using Starcounter.Internal;
using Starcounter.Server.Service;
using Starcounter.Server.Windows;
using System;
using System.IO;
using System.Text;

namespace Starcounter.Server.Setup {
    /// <summary>
    /// Expose the set of properties applicable when installing the
    /// Starcounter system server platform service and a method to
    /// execute the setup.
    /// </summary>
    public sealed class SystemServiceSetup {
        /// <summary>
        /// Gets or sets the Starcounter installation path, needed
        /// to properly resolve the binaries.
        /// </summary>
        public string InstallationPath { get; set; }

        /// <summary>
        /// Gets or sets the service name to use.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the service display name to use.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the service description to use.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the service startup type to use.
        /// </summary>
        public StartupType StartupType { get; set; }

        /// <summary>
        /// Gets or sets the account name under which the
        /// service should run / log in as.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the password to use when the service
        /// needs to log in as <see cref="AccountName"/>.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="SystemServiceSetup"/> with
        /// all default values.
        /// </summary>
        public SystemServiceSetup() {
            InstallationPath = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            ServiceName = ServerService.Name;
            DisplayName = "Starcounter System Service";
            StartupType = StartupType.Manual;
            AccountName = null;
            Password = null;
        }

        /// <summary>
        /// Executes the setup, i.e. installing the system server service
        /// using the specified attributes of the current instance.
        /// </summary>
        /// <returns>The service name of the installed service.</returns>
        public string Execute() {
            var commandLine = new StringBuilder();
            var executable = Path.Combine(this.InstallationPath, "scservice.exe");
            commandLine.Append("\"" + executable + "\"");
            commandLine.Append(" ");
            commandLine.Append("SYSTEM");

            using (var manager = LocalWindowsServiceManager.Open(Win32Service.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG)) {
                return ServerService.Create(
                    manager.Handle,
                    this.DisplayName,
                    this.ServiceName,
                    this.Description,
                    this.StartupType,
                    commandLine.ToString(),
                    this.AccountName,
                    this.Password
                );
            }
        }
    }
}
