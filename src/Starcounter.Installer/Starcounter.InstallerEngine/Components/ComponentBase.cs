using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using Starcounter;

namespace Starcounter.InstallerEngine
{
public abstract class CComponentBase
{
    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public abstract String DescriptiveName
    {
        get;
    }

    /// <summary>
    /// Provides name of the component setting in INI file.
    /// </summary>
    public abstract String SettingName
    {
        get;
    }

    /// <summary>
    /// Provides installation path of the component.
    /// </summary>
    public virtual String ComponentPath
    {
        get
        {
            throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
        }

        set
        {
            throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
        }
    }

    /// <summary>
    /// Installs component.
    /// </summary>
    public abstract void Install();

    /// <summary>
    /// Removes component.
    /// </summary>
    public abstract void Uninstall();

    /// <summary>
    /// Checks if components is already installed.
    /// </summary>
    /// <returns>True if already installed.</returns>
    public abstract Boolean IsInstalled();

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public abstract Boolean CanBeInstalled();

    /// <summary>
    /// Checks if component can be removed.
    /// </summary>
    /// <returns>True if can.</returns>
    public abstract Boolean CanBeRemoved();

    /// <summary>
    /// Determines if this component should be installed
    /// in this session.
    /// </summary>
    /// <returns>True if component should be installed.</returns>
    public abstract Boolean ShouldBeInstalled();

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public abstract Boolean ShouldBeRemoved();

    /// <summary>
    /// Initializes component data.
    /// </summary>
    public virtual void Init()
    {

    }
}
}