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
		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static string GetClassStemIdentifier(Type t) {
			int genericArgCount;
			return GetClassStemIdentifier(t, out genericArgCount);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static string GetClassStemIdentifier(Type t, out int genericArgCount) {
			int index = t.Name.IndexOf('`');
			if (index != -1) {
				genericArgCount = int.Parse(t.Name.Substring(index + 1));
				return t.Name.Substring(0, index);
			} else {
				genericArgCount = 0;
				return t.Name;
			}
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static string GetClassDeclarationSyntax(Type t) {
			int genericArgIndex = 0;
			return GetClassDeclarationSyntax(t, t.GetGenericArguments(), ref genericArgIndex);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
		private static string GetClassDeclarationSyntax(Type t, Type[] genericArgs, ref int genericArgIndex) {
			int genericArgCount;
			string ret = GetClassStemIdentifier(t, out genericArgCount);
			if (genericArgCount > 0) {
				ret = ret + "<";
				for (int gai = 0; gai < genericArgCount; gai++) {
					if (gai > 0)
						ret += ",";

					if (genericArgIndex >= genericArgs.Length)
						throw new Exception("TODO!");
					Type tParam = genericArgs[genericArgIndex++];

					// For type params we need to restart from zero again for the generic index.
					int paramArgIndex = 0;
					ret = ret + GetClassDeclarationSyntax(tParam, tParam.GetGenericArguments(), ref paramArgIndex);
				}
                ret = ret + ">";
            }
            return ret;
        }

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
        internal static void PreLoadCustomDependencies() {

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
                String dllsDir = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);

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

        public static string GetGlobalClassSpecifier(Type type, bool p) {
			int genericArgIndex = 0;
            string ret = type.Namespace;
            if (ret != null && !ret.Equals("")) {
                ret += ".";
            }

			// The type contains all generic arguments, even if it is a nested class (that is
			// this array contains the arguments for the parents as well).
			Type[] genericArgs = type.GetGenericArguments();
            if (type.IsNested) {
                Type pt = type.DeclaringType;
				string owner = GetClassDeclarationSyntax(pt, genericArgs, ref genericArgIndex);
                while ((pt = pt.DeclaringType) != null) {
					owner = GetClassDeclarationSyntax(pt, genericArgs, ref genericArgIndex) + "." + owner;
                }
                ret += owner + ".";
            }
			ret += GetClassDeclarationSyntax(type, genericArgs, ref genericArgIndex);
            return ret;
        }

        /// <summary>
        /// Checks if the given path is on local drive not network.
        /// </summary>
        public static Boolean IsDirectoryLocal(String fullDirPath) {
            DirectoryInfo dirInfo = new DirectoryInfo(fullDirPath);
            String rootFullName = dirInfo.Root.FullName;
            if (rootFullName.StartsWith("\\"))
                return false;

            // Checking each local drive (including mapped network drives).
            foreach (DriveInfo d in DriveInfo.GetDrives()) {
                if (String.Compare(rootFullName, d.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    return (d.DriveType != DriveType.Network);
            }

            return false;
        }
    }
}
