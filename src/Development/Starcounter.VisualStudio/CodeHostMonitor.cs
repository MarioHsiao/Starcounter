﻿using System;
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

        public static CodeHostMonitor Current = new CodeHostMonitor();

        private CodeHostMonitor() {
        }

        public void AssureMonitored(int processId) {
            if (!hosts.ContainsKey(processId)) {
                var p = Process.GetProcessById(processId);
                if (p == null || !IsCodeHost(p.ProcessName)) {
                    throw new Exception(string.Format("Can not monitor process with ID {0}", processId));
                }
                hosts[processId] = p;
            }
        }

        public void ProcessDetatched(int processId, string processName, VsPackage package) {
            if (IsCodeHost(processName)) {
                Process process;
                if (!hosts.TryGetValue(processId, out process)) {
                    // Do what? Warn?
                    // TODO:
                    return;
                }

                process.Refresh();
                if (process.HasExited) {
                    var exitCode = process.ExitCode;
                    if (process.ExitCode > 0) {
                        // Get errors since it has started!
                        // TODO:
                        var log = new FilterableLogReader() {
                            Since = process.StartTime,
                            TypeOfLogs = Severity.Warning
                        };

                        log.Fetch((entry) => {
                            var task = package.ErrorList.NewTask(ErrorTaskSource.Debug, entry.Message, (uint)entry.ErrorCode);
                            task.ErrorCategory = entry.Severity == Severity.Warning ? TaskErrorCategory.Warning : TaskErrorCategory.Error;
                            package.ErrorList.Tasks.Add(task);
                        });
                        
                        package.ErrorList.Refresh();
                        package.ErrorList.Show();
                    }
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
