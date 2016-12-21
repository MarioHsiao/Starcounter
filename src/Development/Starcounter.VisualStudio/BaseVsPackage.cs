using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Starcounter.Internal;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio {
    public abstract class BaseVsPackage : Package {
        static ActivityLogWriter _logWriter = null;

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string fileName);

        /// <summary>
        /// Gets the full path to the Starcounter installation directory.
        /// </summary>
        internal static readonly string InstallationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Gets the full path to Starcounter 32-bit components.
        /// </summary>
        internal static readonly string Installed32BitComponponentsDirectory = Path.Combine(InstallationDirectory, StarcounterEnvironment.Directories.Bit32Components);
        
        static BaseVsPackage() {
            // No logging here! See https://github.com/Starcounter/Starcounter/issues/3781
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            TryExplicitlyLoadingHttpParserBinary();
        }

        private static bool TryExplicitlyLoadingHttpParserBinary() {
            IntPtr moduleHandle;
            string resourceBinaryPath;

            // By design, we catch all possible exceptions here. If any exception
            // occur, we return false.

            moduleHandle = IntPtr.Zero;
            try {
                resourceBinaryPath = InstallationDirectory;
                if (IntPtr.Size == 4) {
                    resourceBinaryPath = Installed32BitComponponentsDirectory;
                }

                moduleHandle = LoadLibrary(Path.Combine(resourceBinaryPath, "schttpparser.dll"));
            } catch { }

            return moduleHandle != IntPtr.Zero;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            // Since we are loaded from a the LoadFrom context, the CLR will not be able
            // to resolve even trivial dependencies. We will help it by scanning the
            // content of the AppDomain and see if the dependency has not been already loaded.
            // Write to the activity log what references fail, and investigate some about
            // how packages are loaded and relate to AppDomains.
            //
            // Here (http://msdn.microsoft.com/en-us/library/bb166359.aspx) is an article
            // about how to write to the activity log.
            // Visual Studio activates the log when the shell receives the /log switch, or
            // when you set an environment variable ("VSLogActivity").
            //
            // Use the log to record high level information for quickly tracking down and
            // routing problems. The log is not a tracing tool - log only key events. When
            // logging is on, the implementation logs an event in response to each method
            // on this interface. When logging is off, the implementation for each method
            // is a fast no-op.
            AssemblyName reference;

            // No logging here! See https://github.com/Starcounter/Starcounter/issues/3781
            
            try
            {
                reference = new AssemblyName(args.Name);
            } catch (FileLoadException) {
                return null;
            }

            // Search for it among loaded assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies) {
                if (AssemblyName.ReferenceMatchesDefinition(reference, assembly.GetName())) {
                    return assembly;
                }
            }

            // Search for it in the Starcounter installation directory, currently
            // being resolved to the place from where this assembly (Starcounter.VisualStudio)
            // has been loaded from.
            string path = null;
            try
            {
                var installationDir = InstallationDirectory;
                if (IntPtr.Size == 4)
                {
                    var pathTo32Bit = Installed32BitComponponentsDirectory;
                    path = Path.Combine(pathTo32Bit, reference.Name + ".dll");
                    if (File.Exists(path))
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(path);
                        return Assembly.Load(assemblyName);
                    }
                }

                path = Path.Combine(installationDir, reference.Name + ".dll");
                if (File.Exists(path))
                {
                    var assemblyName = AssemblyName.GetAssemblyName(path);
                    return Assembly.Load(assemblyName);
                }
            }
            catch { }

            return null;
        }

        protected BaseVsPackage() : base() {
            // No logging here! See https://github.com/Starcounter/Starcounter/issues/3781
            _logWriter = new ActivityLogWriter(this);
        }

        #region Utility methods to support easy logging to the VS activity log

        static void TryWriteLogInformation(string information) {
            if (_logWriter != null) {
                _logWriter.WriteToLog(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, information);
            }
        }

        public void LogInformation(string information) {
            DoWriteToLog(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, information);
        }

        public void LogError(string error) {
            DoWriteToLog(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, error);
        }

        public void LogWarning(string warning) {
            DoWriteToLog(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, warning);
        }

        private void DoWriteToLog(__ACTIVITYLOG_ENTRYTYPE type, string text) {
            IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            if (log != null) {
                log.LogEntry((UInt32)type, this.ToString(), string.Format(CultureInfo.CurrentCulture, text));
            }
        }

        #endregion

        private class ActivityLogWriter {
            BaseVsPackage _package;

            internal ActivityLogWriter(BaseVsPackage package) {
                _package = package;
            }

            internal void WriteToLog(__ACTIVITYLOG_ENTRYTYPE type, string text) {
                _package.DoWriteToLog(type, text);
            }
        }
    }
}