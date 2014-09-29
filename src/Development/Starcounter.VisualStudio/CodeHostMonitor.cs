using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sc.Tools.Logging;
using Starcounter.CLI;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Starcounter.VisualStudio {
    using Severity = Sc.Tools.Logging.Severity;

    internal class MonitoredCodeHostProcess {
        public readonly Process Process;
        public readonly DateTime MonitoredSince;

        public MonitoredCodeHostProcess(Process p) {
            Process = p;
            MonitoredSince = DateTime.Now;
        }
    }

    internal class CodeHostMonitor {
        const string codeHostName = Starcounter.Internal.StarcounterConstants.ProgramNames.ScCode + ".exe";
        Dictionary<int, MonitoredCodeHostProcess> hosts = new Dictionary<int, MonitoredCodeHostProcess>();
        object sync = new object();

        public static CodeHostMonitor Current = new CodeHostMonitor();

        private CodeHostMonitor() {
        }

        public void AssureMonitored(int processId) {
            lock (sync) {
                if (!hosts.ContainsKey(processId)) {
                    var p = Process.GetProcessById(processId);
                    if (p == null || !IsCodeHost(p.ProcessName)) {
                        throw new Exception(string.Format("Can not monitor process with ID {0}", processId));
                    }
                    hosts[processId] = new MonitoredCodeHostProcess(p);
                }
            }
        }

        public void ProcessDetatched(int processId, string processName, VsPackage package) {
            MonitoredCodeHostProcess process;
            Process p;

            if (!IsCodeHost(processName)) {
                return;
            }

            lock (sync) {
                if (!hosts.TryGetValue(processId, out process)) {
                    return;
                }

                p = process.Process;
                p.Refresh();
                if (p.HasExited) {
                    // We can't get the error code; we must check if the log contains
                    // any errors.
                    new ErrorLogDisplay(
                        package, 
                        FilterableLogReader.LogsSince(Severity.Warning, process.MonitoredSince)).ShowInErrorList();
                }

                hosts.Remove(processId);
            }
        }

        bool IsCodeHost(string processName) {
            var result = processName.EndsWith(codeHostName, StringComparison.InvariantCultureIgnoreCase);
            if (!result) {
                result = processName.EndsWith(Path.GetFileNameWithoutExtension(codeHostName), StringComparison.InvariantCultureIgnoreCase);
            }
            return result;
        }
    }
}
