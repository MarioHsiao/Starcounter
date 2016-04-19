using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using System.Diagnostics;
using System.Configuration.Install;
using Starcounter;
using Starcounter.Internal;

namespace Starcounter.InstallerEngine
{
public class CInstallationBase : CComponentBase
{
    /// <summary>
    /// Component initialization.
    /// </summary>
    public CInstallationBase()
    {
    }

    /// <summary>
    /// Provides installation path of the component.
    /// </summary>
    public override String ComponentPath
    {
        get
        {
            return InstallerMain.InstallationDir;
        }
    }

    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public override String DescriptiveName
    {
        get
        {
            return "Starcounter Installation Base";
        }
    }

    /// <summary>
    /// Provides name of the component setting.
    /// </summary>
    public override String SettingName
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the directory path where Starcounter is installed,
    /// obtained from environment variables.
    /// </summary>
    public static String GetInstalledDirFromEnv()
    {
        // First checking the user-wide installation directory.
        String scInstDir = Environment.GetEnvironmentVariable(ConstantsBank.SCEnvVariableName,
            EnvironmentVariableTarget.User);

        if (scInstDir != null)
            return scInstDir;

        // Then checking the system-wide installation directory.
        scInstDir = Environment.GetEnvironmentVariable(ConstantsBank.SCEnvVariableName,
            EnvironmentVariableTarget.Machine);

        return scInstDir;
    }

    /// <summary>
    /// Returns environment variable value from user/machine level.
    /// </summary>
    public static String GetEnvVarMachineUser(String envVarName)
    {
        // First checking the user level.
        String varValue = Environment.GetEnvironmentVariable(envVarName,
            EnvironmentVariableTarget.User);

        if (varValue != null)
            return varValue;

        // Then checking the machine level.
        varValue = Environment.GetEnvironmentVariable(envVarName,
            EnvironmentVariableTarget.Machine);

        return varValue;
    }

    // Adds firewall exceptions to certain executables.
    static String[] FirewallExceptionPrograms = 
    {
        "32BitComponents\\" + StarcounterConstants.ProgramNames.ScSqlParser,
        StarcounterConstants.ProgramNames.ScNetworkGateway,
        StarcounterConstants.ProgramNames.ScNetworkGateway,
        StarcounterConstants.ProgramNames.ScNetworkGateway,
        StarcounterConstants.ProgramNames.ScNetworkGateway,
        StarcounterConstants.ProgramNames.ScNetworkGateway
    };

    // Firewall exceptions names.
    static String[] FirewallExceptionNames = 
    {
        StarcounterConstants.ProgramNames.ScSqlParser,
        StarcounterConstants.ProgramNames.ScNetworkGateway + StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort_String,
        StarcounterConstants.ProgramNames.ScNetworkGateway + StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort_String,
        StarcounterConstants.ProgramNames.ScNetworkGateway + StarcounterConstants.NetworkPorts.DefaultSystemServerUserHttpPort_String,
        StarcounterConstants.ProgramNames.ScNetworkGateway + StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort_String,
        StarcounterConstants.ProgramNames.ScNetworkGateway + MixedCodeConstants.GatewayAggregationPortSettingName
    };

    /// <summary>
    /// Adds/Removes Windows firewall exceptions.
    /// </summary>
    /// <param name="isAdding"></param>
    void ChangeFirewallExceptions(Boolean isAdding)
    {
        // Logging event.
        Utilities.ReportSetupEvent("Changing Windows Firewall exceptions for Starcounter components...");

        String[] FirewallSpecialParams = null;
        if (isAdding)
        {
            // Special firewall rules for each program.
            FirewallSpecialParams = new String[FirewallExceptionPrograms.Length];

            FirewallSpecialParams[0] = "remoteip=127.0.0.1";
            FirewallSpecialParams[1] = "protocol=TCP localport=" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerUserHttpPort);
            FirewallSpecialParams[2] = "protocol=TCP localport=" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerSystemHttpPort);
            FirewallSpecialParams[3] = "protocol=TCP localport=" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerUserHttpPort);
            FirewallSpecialParams[4] = "protocol=TCP localport=" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerSystemHttpPort);
            FirewallSpecialParams[5] = "protocol=TCP localport=" + InstallerMain.GetInstallationSettingValue(MixedCodeConstants.GatewayAggregationPortSettingName);
        }

        // Adding each executable as an exception.
        for (Int32 i = 0; i < FirewallExceptionPrograms.Length; i++)
        {
            // Combining installation path with executable name.
            String exeFullPath = Path.Combine(ComponentPath, FirewallExceptionPrograms[i] + ".exe");

            // Using "netsh" tool to configure the firewall.
            Process netshTool = new Process();
            try
            {
                netshTool.StartInfo.FileName = "netsh";
                netshTool.StartInfo.UseShellExecute = false;
                netshTool.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                netshTool.StartInfo.CreateNoWindow = true;

                // Adding program to the firewall.
                if (isAdding)
                {
                    // Creating rule from executable name without path!
                    netshTool.StartInfo.Arguments = 
                        "advfirewall firewall add rule name=\"Allow " + FirewallExceptionNames[i] + "\" " +
                        "description=\"Allow inbound traffic for one of the Starcounter components.\" " +
                        FirewallSpecialParams[i] + " dir=in program=\"" + exeFullPath + "\" action=allow";
                }
                else // Removing program from the firewall.
                {
                    netshTool.StartInfo.Arguments = "advfirewall firewall delete rule name=\"Allow " + FirewallExceptionNames[i] + "\"";
                }

                netshTool.Start();
                netshTool.WaitForExit(60000);

                if ((!netshTool.HasExited) || (netshTool.ExitCode != 0))
                {
                    if (isAdding)
                    {
                        String firewallPanicMsg = "Starcounter installer encountered a problem adding firewall exception for: " + Environment.NewLine +
                                                  exeFullPath + Environment.NewLine +
                                                  "Please check that your firewall service is enabled or add the 'Allow' rule manually.";

                        if (InstallerMain.SilentFlag)
                        {
                            // Printing a console message.
                            Utilities.ConsoleMessage(firewallPanicMsg);

                            // Temporary replaced error with message box info.
                            throw ErrorCode.ToException(Error.SCERRINSTALLERFIREWALLEXCEPTION,
                                "Executable which causes problems: " + exeFullPath);
                        }
                        else
                        {
                            Utilities.MessageBoxInfo(firewallPanicMsg,
                                "Problems adding Windows Firewall exception...");
                        }
                    }
                }
            }
            finally
            {
                netshTool.Close();
            }
        }
    }

    /// <summary>
    /// Installs component.
    /// </summary>
    public override void Install()
    {
        // Checking if component should be installed in this session.
        if (!ShouldBeInstalled())
            return;

        // Checking that component is not already installed.
        if (!CanBeInstalled())
            return;

        // Checking that components directories are different.
        if (!Utilities.RunningOnBuildServer())
        {
            if (Path.GetDirectoryName(InstallerMain.PersonalServerComponent.ComponentPath + "\\").Equals(Path.GetDirectoryName(InstallerMain.InstallationBaseComponent.ComponentPath + "\\"), StringComparison.InvariantCultureIgnoreCase))
            {
                Utilities.MessageBoxError("At least two components have equal installation directories. All components should be installed in different directories.", "Equal installation directories...");
                throw ErrorCode.ToException(Error.SCERRINSTALLERSAMEDIRECTORIES);
            }
        }

        // Logging event.
        Utilities.ReportSetupEvent("Setting rights for current user on installation directory...");
        Utilities.AddDirFullPermissionsForCurrentUser(InstallerMain.InstallationDir);

        // Logging event.
        Utilities.ReportSetupEvent("Creating environment variables for installation base...");

        // Path to .NET framework that we add in installer.
        String frameworkPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

        // No matter what type of installation is it we need to create/overwrite
        // StarcounterBin user environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName, ComponentPath, EnvironmentVariableTarget.User);
        PathVariable.AddPath(ComponentPath, EnvironmentVariableTarget.User);
        PathVariable.AddPath(frameworkPath, EnvironmentVariableTarget.User);

        // Also setting variable for current process (so that subsequently
        // started processes can find the installation path).
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName, ComponentPath, EnvironmentVariableTarget.Process);
        PathVariable.AddPath(ComponentPath, EnvironmentVariableTarget.Process);
        PathVariable.AddPath(frameworkPath, EnvironmentVariableTarget.Process);

        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName, ComponentPath, EnvironmentVariableTarget.Machine);
        PathVariable.AddPath(ComponentPath, EnvironmentVariableTarget.Machine);
        PathVariable.AddPath(frameworkPath, EnvironmentVariableTarget.Machine);

        // Logging event.
        Utilities.ReportSetupEvent("Creating base Start Menu items...");

        // Obtaining path to Start Menu for a current user.
        String startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            @"Programs\" + ConstantsBank.SCProductName);

        // Creating Start Menu directory if needed.
        if (!Directory.Exists(startMenuDir))
            Directory.CreateDirectory(startMenuDir);

        // Installation configuration directory
        var configDir = Path.Combine(InstallerMain.InstallationDir, StarcounterEnvironment.Directories.InstallationConfiguration);
        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(ComponentPath, ConstantsBank.SCInstallerGUI + ".exe"),
            Path.Combine(startMenuDir, "Add or Remove Starcounter Components.lnk"),
            "",
            ComponentPath,
            "Used to add and remove components or uninstall Starcounter completely.",
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCIconFilename));

        // Logging event.
        Utilities.ReportSetupEvent("Adding Starcounter to the 'Add/Remove Programs' list...");

        // Creating entry for current user only.
        RegistryKey rk = Utilities.CreateRegistryPathIfNeeded(ConstantsBank.Registry64BitUninstallPath, false);

        // Creating necessary registry entries representing Starcounter installation.
        RegistryKey scRk = rk.CreateSubKey(ConstantsBank.SCProductName);
        scRk.SetValue("DisplayIcon", Path.Combine(ComponentPath, ConstantsBank.SCIconFilename));
        scRk.SetValue("DisplayName", ConstantsBank.SCProductName);
        scRk.SetValue("DisplayVersion", CurrentVersion.Version);
        scRk.SetValue("InstallLocation", ComponentPath);
        scRk.SetValue("Publisher", "Starcounter AB");

        // Referring to Starcounter Setup executable.
        scRk.SetValue("UninstallString", Path.Combine(ComponentPath, ConstantsBank.SCInstallerGUI + ".exe"));

        scRk.Close();

        // Adding firewall exceptions.
        ChangeFirewallExceptions(true);

        // Adding public assemblies registry path.
        RegistryKey refAsmRegistry = Utilities.CreateRegistryPathIfNeeded(@"SOFTWARE\Wow6432Node\Microsoft\.NetFramework\v4.5\AssemblyFoldersEx\" + ConstantsBank.SCProductName + CurrentVersion.Version, true);
        refAsmRegistry.SetValue(null, Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCPublicAssembliesDir));

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Removes component.
    /// </summary>
    public override void Uninstall()
    {
        if (!UninstallEngine.CompleteCleanupSetting)
        {
            if (UninstallEngine.RollbackSetting)
            {
                // Checking if component was installed in this session.
                if (!ShouldBeInstalled())
                    return;
            }
            else // Standard removal.
            {
                // Checking if component is selected to be removed.
                if (!ShouldBeRemoved())
                    return;
            }

            // Checking if component can be removed.
            if (!CanBeRemoved())
                return;
        }

        // Logging event.
        Utilities.ReportSetupEvent("Deleting Starcounter base environment variables...");

        // Path to .NET framework that we add in installer.
        String frameworkPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

        // Removing environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName, null, EnvironmentVariableTarget.User);
        PathVariable.RemovePath(ComponentPath, EnvironmentVariableTarget.User);
        PathVariable.RemovePath(frameworkPath, EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName, null, EnvironmentVariableTarget.Machine);
        PathVariable.RemovePath(ComponentPath, EnvironmentVariableTarget.Machine);
        PathVariable.RemovePath(frameworkPath, EnvironmentVariableTarget.Machine);

        // Logging event.
        Utilities.ReportSetupEvent("Deleting Starcounter entry from the 'Add/Remove Programs' list...");

        // Removes Starcounter entry from the "Add/Remove Programs" list.
        RegistryKey rk = Registry.CurrentUser.OpenSubKey(ConstantsBank.Registry64BitUninstallPath, true);
        if ((rk != null) && (rk.OpenSubKey(ConstantsBank.SCProductName) != null))
            rk.DeleteSubKeyTree(ConstantsBank.SCProductName);

        // Logging event.
        Utilities.ReportSetupEvent("Deleting Starcounter Start Menu items...");

        // Path to Starcounter Start Menu items.
        String startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), @"Programs\" + ConstantsBank.SCProductName);

        // Obtaining path to Start Menu for a current user.
        Utilities.ForceDeleteDirectory(new DirectoryInfo(startMenuPath));

        // Removing firewall exceptions.
        ChangeFirewallExceptions(false);

        // Removing public assemblies registry path.
        RegistryKey refAsmRegistry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\.NetFramework\v4.5\AssemblyFoldersEx", true);
        if ((refAsmRegistry != null) && (refAsmRegistry.OpenSubKey(ConstantsBank.SCProductName + CurrentVersion.Version) != null))
            refAsmRegistry.DeleteSubKeyTree(ConstantsBank.SCProductName + CurrentVersion.Version);
    }

    /// <summary>
    /// Checks if components is already installed.
    /// </summary>
    /// <returns>True if already installed.</returns>
    public override Boolean IsInstalled()
    {
        // Checking for Starcounter environment variables existence (both current user and system-wide).
        String envVarUser = Environment.GetEnvironmentVariable(ConstantsBank.SCEnvVariableName, EnvironmentVariableTarget.User);
        String envVarMachine = Environment.GetEnvironmentVariable(ConstantsBank.SCEnvVariableName, EnvironmentVariableTarget.Machine);

        // If any of them exists then Starcounter is installed.
        if ((envVarUser != null) || (envVarMachine != null))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeInstalled()
    {
        return (!IsInstalled());
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeRemoved()
    {
        return (UninstallEngine.RemainingComponents() <= 0);
    }

    /// <summary>
    /// Determines if this component should be installed
    /// in this session.
    /// </summary>
    /// <returns>True if component should be installed.</returns>
    public override Boolean ShouldBeInstalled()
    {
        // NOTE: Installation base should always be installed.
        return true;

        /*
        // Mapping boolean flags to settings.
        Boolean installPersonalServerSetting = InstallerMain.PersonalServerComponent.ShouldBeInstalled();
        Boolean installSystemServerSetting = InstallerMain.SystemServerComponent.ShouldBeInstalled();
        Boolean installVs2012IntegrationSetting = InstallerMain.VS2012IntegrationComponent.ShouldBeInstalled();

        // Depending on the list of installing components
        // we determine if installation base is needed.
        if (installPersonalServerSetting        ||
            installSystemServerSetting          ||
            installVs2012IntegrationSetting)
            return true;

        return false;*/
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return CanBeRemoved();
    }
}
}