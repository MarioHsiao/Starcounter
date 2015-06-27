
using System;
using System.Diagnostics;
using System.IO;

namespace Starcounter.VS14.Extension.Sample
{
    /// <summary>
    /// <para>
    /// Normally, installing VSIX extensions is done either by double-clicking
    /// the .vsix file -- effectively invoking the OS-level VSIX installer -- or
    /// by means of the VS "Extension manager", under "Tools\Extension Manager"
    /// in Visual Studio.
    /// </para>
    /// <para>
    /// Since we provide a custom Starcounter installer, we want to make sure we
    /// confirm to the same kind of installation logic as the above two and hence
    /// we utilize the Visual Studio VSIXInstaller.exe tool. This sample class
    /// shows how to invoke this tool programmatically and the parameters we are
    /// to use.
    /// </para>
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The folder where we expect Visual Studio binaries to reside.
        /// </summary>
        const string VisualStudioFolder = @"c:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE";

        /// <summary>
        /// The name of the VSIX installer executable.
        /// </summary>
        const string VSIXInstallerName = "VSIXInstaller.exe";

        /// <summary>
        /// The .vsix package file we install. This is the principal output of
        /// this project, i.e. the useful artifact we'll use (as compared to this
        /// sample code, that is just that - a (runnable) sample).
        /// </summary>
        const string VSIXPackageFile = "Starcounter.VS14.vsix";

        /// <summary>
        /// The unique ID we use for our extension. Used when uninstalling. The
        /// main definition of this ID is the .vsixmanifest file part of this
        /// project. Make sure they are in-sync.
        /// </summary>
        const string VSIXExtensionID = "Starcounter.VS14.96DB7E43-995A-4754-BDDB-7E37EDB2281B";

        /// <summary>
        /// Visual Studio versions we support installing into. That is, even if
        /// we use the installer of VS 11, we can still install our extension into
        /// both VS 2010 and VS 11.
        /// </summary>
        enum VisualStudioVersion
        {
            VS2010,
            VS11,
            VS12,
            VS14
        }

        /// <summary>
        /// Visual studio editions we support installing into.
        /// </summary>
        enum VisualStudioEdition
        {
            Ultimate,
            Premium,
            Pro,
            Community
        }
        
        /// <summary>
        /// Program entrypoint, running an install/uninstall pair to show the
        /// roundtrip of installing and uninstalling programatically.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var installer = Path.Combine(VisualStudioFolder, VSIXInstallerName);
            var edition = VisualStudioEdition.Community.ToEditionString();
            var version = VisualStudioVersion.VS14.ToVersionString();

            Install(installer, edition, version);
            Uninstall(installer, edition, version);
        }

        static void Install(string installer, string edition, string version)
        {
            
            string arguments = string.Format(
                "/quiet /skuName:{0} /skuVersion:{1} \"{2}\"", 
                edition, 
                version, 
                Path.GetFullPath(VSIXPackageFile)
                );

            // Example command-line:
            // VSIXInstaller.exe /quiet /skuName:Pro /skuVersion:14.0 Starcounter.VS14.Extension.vsix
            //
            // Explanation:
            // Uninstall the extension described in the Starcounter.VS14.Extension.vsix file into Visual
            // Studio 2015 Community.
            
            Process.Start(installer, arguments).WaitForExit();
        }

        static void Uninstall(string installer, string edition, string version)
        {
            string arguments = string.Format(
                "/quiet /skuName:{0} /skuVersion:{1} /uninstall:{2}",
                edition,
                version,
                VSIXExtensionID
                );

            // Example command-line:
            // VSIXInstaller.exe /quiet /skuName:Pro /skuVersion:14.0 /uninstall:Starcounter.VS14.96DB7E43-995A-4754-BDDB-7E37EDB2281B
            //
            // Explanation:
            // Uninstall the extension with ID "Starcounter.VS14.96DB7E43-995A-4754-BDDB-7E37EDB2281B" from
            // Visual Studio 2015 Community.

            Process.Start(installer, arguments).WaitForExit();
        }

        static string ToEditionString(this VisualStudioEdition edition)
        {
            return Enum.GetName(typeof(VisualStudioEdition), edition);
        }

        static string ToVersionString(this VisualStudioVersion version)
        {
            if (version == VisualStudioVersion.VS2010) return "10.0";
            if (version == VisualStudioVersion.VS11) return "11.0";
            if (version == VisualStudioVersion.VS12) return "12.0";
            if (version == VisualStudioVersion.VS14) return "14.0";
            throw new Exception("Unknown version!");
        }
    }
}
