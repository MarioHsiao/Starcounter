﻿using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Starcounter.VisualStudio {
    public abstract class BaseVsPackage : Package {
        static ActivityLogWriter _logWriter = null;

        static BaseVsPackage() {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
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
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            TryWriteLogInformation(string.Format("Resolving assembly {0}", args.Name));
            try {
                reference = new AssemblyName(args.Name);
            } catch (FileLoadException) {
                return null;
            }
            foreach (Assembly assembly in assemblies) {
                if (AssemblyName.ReferenceMatchesDefinition(reference, assembly.GetName())) {
                    return assembly;
                }
            }

#if false
        try
        {
            //string binDir = StarcounterEnvironment.SystemDirectory;
            //string path = Path.Combine(binDir, reference.Name + ".dll");
            //if (File.Exists(path))
            //{
            //    var assemblyName = AssemblyName.GetAssemblyName(path);
            //    return Assembly.Load(assemblyName);
            //}
        }
        catch { }
#endif
            return null;
        }

        protected BaseVsPackage()
            : base() {
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