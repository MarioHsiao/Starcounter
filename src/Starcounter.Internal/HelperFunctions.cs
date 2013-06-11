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
        static void LoadDLL(String dllName) {

            // Get the location of current executing assembly.
            String tempDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), dllName);
            if (File.Exists(tempDllPath)) {
                dllName = tempDllPath;
                goto LOAD_DLL;
            }
                
            // Since that didn't work trying StarcounterBin.
            String starcounterBin = System.Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);

            // Checking if its 32-bit process.
            if (4 == IntPtr.Size)
                dllName = Path.Combine(starcounterBin, StarcounterEnvironment.Directories.Bit32Components, dllName);
            else
                dllName = Path.Combine(starcounterBin, dllName);

LOAD_DLL:

            if (!File.Exists(dllName))
                throw new FileNotFoundException("DLL not found: " + dllName);

            IntPtr moduleHandle = LoadLibrary(dllName);

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

                foreach (String dllName in AllPreloadLibraries) {
                    LoadDLL(dllName);
                }

                dllsLoaded_ = true;
            }
        }
    }
}
