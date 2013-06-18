using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal
{
    public class HelperFunctions
    {
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(String filePath);

        /// <summary>
        /// Loads specified library.
        /// </summary>
        /// <param name="dllName">Name of the DLL to load.</param>
        static void LoadDLL(String dllName, String dllsDir) {

            dllName = Path.Combine(dllsDir, dllName);

            // Checking if DLL is on the disk.
            if (!File.Exists(dllName))
                throw new FileNotFoundException("DLL not found: " + dllName);

            IntPtr moduleHandle = LoadLibrary(dllName);

            // Checking if DLL loaded correctly.
            if (IntPtr.Zero == moduleHandle)
                throw new Exception("Can't load DLL: " + dllName);
        }

        static Boolean disableAssembliesPreLoading_;

        /// <summary>
        /// Disable assemblies preloading.
        /// </summary>
        public static void DisableAssembliesPreLoading()
        {
            disableAssembliesPreLoading_ = true;
        }

        /// <summary>
        /// Native assemblies to pre-load.
        /// </summary>
        static readonly String[] NativeAssemblies = {
            "scerrres.dll",
            "schttpparser.dll"            
        };

        /// <summary>
        /// Object used for locking.
        /// </summary>
        static Object lockObject_ = new Object();

        /// <summary>
        /// Indicates if DLLs are loaded.
        /// </summary>
        static Boolean dllsLoaded_;

        /// <summary>
        /// Load non-GAC library dependencies.
        /// </summary>
        internal static void LoadNonGACDependencies() {

            if (disableAssembliesPreLoading_)
                return;

            if (dllsLoaded_)
                return;

            lock (lockObject_) {

                if (dllsLoaded_)
                    return;

                // Checking if DLLs are in the same directory as current assembly and if yes - not loading them.
                // Primarily this is used when building Level1 which uses XSON code generation.
                String tempDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), NativeAssemblies[0]);
                if (File.Exists(tempDllPath))
                    goto DLLS_LOADED;

                // Trying StarcounterBin.
                String dllsDir = System.Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);

                // Checking that variable exists.
                if (String.IsNullOrEmpty(dllsDir))
                    throw new Exception("Starcounter is not installed properly. StarcounterBin environment variable is missing.");

                // Checking if its 32-bit process.
                if (4 == IntPtr.Size)
                    dllsDir = Path.Combine(dllsDir, StarcounterEnvironment.Directories.Bit32Components);

                // Adding custom assembly resolving directory.
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
                {
                    AssemblyName asmName = new AssemblyName(args.Name);
                    String destPathToDll = Path.Combine(dllsDir, asmName.Name + ".dll");

                    if (File.Exists(destPathToDll))
                        return Assembly.LoadFile(destPathToDll);

                    return null;
                };

                // Running throw all native assemblies.
                foreach (String dllName in NativeAssemblies) {
                    LoadDLL(dllName, dllsDir);
                }

DLLS_LOADED:

                dllsLoaded_ = true;
            }
        }
    }
}
