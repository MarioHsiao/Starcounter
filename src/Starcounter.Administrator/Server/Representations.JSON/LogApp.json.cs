using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sc.Tools.Logging;
using Starcounter;
using System.Collections;

namespace Starcounter.Administrator.Server {
    partial class LogApp : Json {

        static string directoryPath;

        static public void Setup(string directory) {
            directoryPath = directory;
        }

        public void RefreshLogEntriesList() {

            Hashtable activeFilterSourceList = new Hashtable();
            Hashtable filterSourceList = new Hashtable();

            if (!string.IsNullOrEmpty(this.FilterSource)) {
                string[] sourceFilter = this.FilterSource.Split(';');

                foreach (string source in sourceFilter) {
                    if (!activeFilterSourceList.ContainsKey(source.ToUpper())) {
                        activeFilterSourceList.Add(source.ToUpper(), source);
                    }
                }

            }

            this.LogEntries.Clear(); // Clearlist

 

            long limit = 30;   // Limith the result

            if (this.FilterMaxItems > 0) {
                limit = this.FilterMaxItems;
            }

            var lr = new LogReader();
            var i = 0;
            lr.Open(directoryPath, ReadDirection.Reverse, (64 * 1024));
            for (; ; ) {
                var le = lr.Next();
                if (le == null) break;

                // Add Source filter items
                if (!filterSourceList.ContainsKey(le.Source)) {
                    filterSourceList.Add(le.Source, le.Source);
                }

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

                if (!string.IsNullOrEmpty(this.FilterSource)) {
                    // Use source filter
                    if (!activeFilterSourceList.ContainsKey(le.Source.ToUpper())) {
                        continue;
                    }
                }


                if (++i > limit) break;

                LogEntries.Add(
                    new LogEntriesElementJson() {
                        DateTimeStr = le.DateTime.ToString(),
                        TypeStr = le.Severity.ToString(),
                        HostName = le.HostName,
                        Source = le.Source,
                        Message = le.Message
                    }
                    );
            }

            string sourceItems = string.Empty;
            foreach (DictionaryEntry item in filterSourceList) {
                if (sourceItems != string.Empty) {
                    sourceItems += ";";
                }
                sourceItems += item.Value.ToString();
            }

            this.FilterSource = sourceItems;

            lr.Close();
        }
    }
}