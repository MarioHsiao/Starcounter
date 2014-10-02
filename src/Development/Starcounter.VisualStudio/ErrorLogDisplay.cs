
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sc.Tools.Logging;
using Starcounter.CLI;
using Starcounter.Internal;
using System;

namespace Starcounter.VisualStudio {
    using Severity = Sc.Tools.Logging.Severity;

    internal sealed class ErrorLogDisplay {
        readonly VsPackage package;
        readonly FilterableLogReader log;

        public ErrorLogDisplay(VsPackage package, FilterableLogReader logReader) {
            this.package = package;
            log = logReader;
        }

        public void ShowInErrorList() {
            var debugOutput = package.DebugOutputPane;
            var snapshot = LogSnapshot.Take(log);

            foreach (var entry in snapshot.All) {
                WriteLogEntryToOuput(entry, package, debugOutput);
            }

            package.ErrorList.Refresh();
            package.ErrorList.Show();
            debugOutput.Activate();
        }

        void WriteLogEntryToOuput(LogEntry entry, VsPackage package, IVsOutputWindowPane debugOutput) {
            StarcounterErrorTask task;
            string debugOutputMsg;

            try {
                var msg = ErrorMessage.Parse(entry.Message);
                task = package.ErrorList.NewTask(ErrorTaskSource.Debug, msg);
                debugOutputMsg = msg.Message;
            } catch {
                task = package.ErrorList.NewTask(ErrorTaskSource.Debug, entry.Message, (uint)entry.ErrorCode);
                debugOutputMsg = entry.Message;
            }

            task.ErrorCategory = entry.Severity == Severity.Warning ? TaskErrorCategory.Warning : TaskErrorCategory.Error;
            package.ErrorList.Tasks.Add(task);
            if (entry.Severity != Severity.Warning) {
                debugOutput.OutputString(debugOutputMsg + Environment.NewLine);
            }
        }
    }
}
