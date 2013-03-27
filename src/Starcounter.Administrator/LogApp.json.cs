using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sc.Tools.Logging;
using Starcounter;

namespace Starcounter.Administrator {
    partial class LogApp : Json {

        static string directoryPath;

        static public void Setup(string directory) {
            directoryPath = directory;
        }

        public void RefreshLogEntriesList() {

            this.LogEntries.Clear(); // Clearlist

            int limit = 30;   // Limith the result

            var lr = new LogReader();
            var i = 0;
            lr.Open(directoryPath, ReadDirection.Reverse, (64 * 1024));
            for (; ; ) {
                var le = lr.Next();
                if (le == null) break;

                if (this.FilterDebug == false && le.Severity == Severity.Debug) {
                    continue;
                }

                if (this.FilterWarning == false && le.Severity == Severity.Warning) {
                    continue;
                }

                if (this.FilterNotice == false && (le.Severity == Severity.Notice || le.Severity == Severity.SuccessAudit)) {
                    continue;
                }

                if (this.FilterError == false && (le.Severity == Severity.Error || le.Severity == Severity.FailureAudit || le.Severity == Severity.Critical)) {
                    continue;
                }

                if (++i > limit) break;

                LogEntries.Add(
                    new LogEntryApp() {
                        DateTimeStr = le.DateTime.ToString(),
                        TypeStr = le.Severity.ToString(),
                        HostName = le.HostName,
                        Source = le.Source,
                        Message = le.Message
                    }
                    );
            }
            lr.Close();
        }
    }
}