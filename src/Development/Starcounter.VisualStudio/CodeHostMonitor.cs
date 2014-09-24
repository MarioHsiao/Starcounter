using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Starcounter.CLI;
using Sc.Tools.Logging;
using Microsoft.VisualStudio.Shell;
using System.IO;

namespace Starcounter.VisualStudio {

    internal class CodeHostMonitor {
        const string codeHostName = Starcounter.Internal.StarcounterConstants.ProgramNames.ScCode + ".exe";
        Dictionary<int, Process> hosts = new Dictionary<int, Process>();
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
                    hosts[processId] = p;
                }
            }
        }

        public void ProcessDetatched(int processId, string processName, VsPackage package) {
            Process process;

            if (!IsCodeHost(processName)) {
                return;
            }

            lock (sync) {
                if (!hosts.TryGetValue(processId, out process)) {
                    return;
                }

                process.Refresh();
                if (process.HasExited) {
                    // We can't get the error code; we must check if the log contains
                    // any errors.

                    var log = new FilterableLogReader() {
                        Count = 10,
                        Since = process.StartTime,
                        TypeOfLogs = Severity.Warning
                    };
                    var debugOutput = package.DebugOutputPane;

                    log.Fetch((entry) => {
                        var task = package.ErrorList.NewTask(ErrorTaskSource.Debug, entry.Message, (uint)entry.ErrorCode);
                        task.ErrorCategory = entry.Severity == Severity.Warning ? TaskErrorCategory.Warning : TaskErrorCategory.Error;
                        package.ErrorList.Tasks.Add(task);
                        if (entry.Severity != Severity.Warning) {
                            debugOutput.OutputString(entry.Message);
                        }
                    });

                    package.ErrorList.Refresh();
                    package.ErrorList.Show();
                    debugOutput.Activate();
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
