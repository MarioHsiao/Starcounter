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
    /// <summary>
    /// Resolves Starcounter libraries from Starcounter bin directory.
    /// </summary>
    public class StarcounterResolver {

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(String filePath);

        /// <summary>
        /// Name of env var that points to Starcounter binaries.
        /// </summary>
        const String StarcounterBinEnvVar = "StarcounterBin";

        /// <summary>
        /// 32BitComponents directory.
        /// </summary>
        const String Bit32Components = "32BitComponents";

        /// <summary>
        /// Loads specified library from the file system.
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
        static Boolean triedToLoad_;

        /// <summary>
        /// Load Starcounter library dependencies.
        /// </summary>
        static public Int32 LoadDependencies() {

            // Checking if already loaded the DLLs.
            if (triedToLoad_)
                return 1;

            // Locking so no calls are made simultaneously.
            lock (lockObject_) {

                // Checking if already loaded the DLLs.
                if (triedToLoad_)
                    return 1;

                // Indicating that we already tried to load.
                triedToLoad_ = true;

                // Checking if DLLs are in the same directory as current assembly and if yes - not loading them.
                // Primarily this is used when building Level1 which uses XSON code generation.
                String tempDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), NativeAssemblies[0]);
                if (File.Exists(tempDllPath))
                    return 2;

                // Trying StarcounterBin.
                String dllsDir = Environment.GetEnvironmentVariable(StarcounterBinEnvVar);

                // Checking that directory exists (if does not exist simply returning).
                if (!Directory.Exists(dllsDir))
                    return 3;

                // Checking if its 32-bit process.
                if (4 == IntPtr.Size)
                    dllsDir = Path.Combine(dllsDir, Bit32Components);

                // Checking that directory exists (if does not exist simply returning).
                if (!Directory.Exists(dllsDir))
                    return 4;

                // Adding custom assembly resolving directory.
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => {

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

                return 0;
            }
        }
    }
}
