using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal
{
    internal class HelperFunctions
    {
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(String filePath);

        /// <summary>
        /// Loads specified library.
        /// </summary>
        /// <param name="dllName">Name of the DLL to load.</param>
        static void LoadDLL(String dllName, String starcounterBin) {

            // Checking if its 32-bit process.
            if (4 == IntPtr.Size)
                dllName = Path.Combine(starcounterBin, StarcounterEnvironment.Directories.Bit32Components, dllName);
            else
                dllName = Path.Combine(starcounterBin, dllName);

            // Checking if DLL is on the disk.
            if (!File.Exists(dllName))
                throw new FileNotFoundException("DLL not found: " + dllName);

            IntPtr moduleHandle = LoadLibrary(dllName);

            // Checking if DLL loaded correctly.
            if (IntPtr.Zero == moduleHandle)
                throw new Exception("Can't load DLL: " + dllName);
        }

        /// <summary>
        /// All libraries to pre-load.
        /// </summary>
        static readonly String[] AllPreloadLibraries = {
            "Mono.CSharp.dll",
            "schttpparser.dll",
            "scerrres.dll"
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

            if (dllsLoaded_)
                return;

            lock (lockObject_) {

                if (dllsLoaded_)
                    return;

                // Checking if DLLs are in the same directory as current assembly and if yes - not loading them.
                // Primarily this is used when building Level1 which uses XSON code generation.
                String tempDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AllPreloadLibraries[0]);
                if (File.Exists(tempDllPath))
                    goto DLLS_LOADED;

                // Trying StarcounterBin.
                String starcounterBin = System.Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);

                // Checking that variable exists.
                if (String.IsNullOrEmpty(starcounterBin))
                    throw new Exception("Starcounter is not installed properly. StarcounterBin environment variable is missing.");

                foreach (String dllName in AllPreloadLibraries) {
                    LoadDLL(dllName, starcounterBin);
                }

DLLS_LOADED:

                dllsLoaded_ = true;
            }
        }
    }
}
