using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sc.Tools.Logging;
using Starcounter;

namespace StarcounterApps3 {
    partial class LogApp : App {

        static List<LogEntry> GlobalLogEntries = new List<LogEntry>();

        static public void Setup(string directory) {
            ThreadPool.QueueUserWorkItem(LogListenerThread, directory);
        }

        static private void LogListenerThread(object state) {

            String directory = state as String;

            LogFilter lf;
            LogReader lr;

            if (!Directory.Exists(directory)) {
                Console.WriteLine("Specified directory does not exist.");
                return;
            }

            try {

                lf = null;
                lr = new LogReader(directory, lf, (4096 * 256));
                lr.Open();
                //int i = 0;
                LogEntry le;
                for (; ; ) {
                    le = lr.Read(true);
                    if (le == null) {
                        break;
                    }

                    LogApp.GlobalLogEntries.Add(le);
                }
                lr.Close();
            } catch (Exception) {
            }


        }

        void Handle(Input.RefreshList action) {
            this.RefreshLogEntriesList();
        }

        void Handler(Input.FilterNotice action) {
            this.RefreshLogEntriesList();
        }

        void Handler(Input.FilterWarning action) {
            this.RefreshLogEntriesList();
        }

        void Handler(Input.FilterError action) {
            this.RefreshLogEntriesList();
        }

        void Handle(Input.FilterDebug action) {
            this.RefreshLogEntriesList();
        }


        public void RefreshLogEntriesList() {

            this.LogEntries.Clear(); // Clearlist

            this.ResetSummary();

            int limith = 30;   // Limith the result


            LogEntryApp logEntryApp;
            for (int i = (GlobalLogEntries.Count - 1); i > 0; i--) {

                LogEntry le = GlobalLogEntries[i];

                // Summerize
                if (le.Type == EntryType.Debug) {
                    this.Summary.Debug++;
                }

                if (le.Type == EntryType.Notice) {
                    this.Summary.Notice++;
                }
                if (le.Type == EntryType.Warning) {
                    this.Summary.Warning++;
                }

                if (le.Type == EntryType.Error || le.Type == EntryType.FailureAudit || le.Type == EntryType.Critical) {
                    this.Summary.Errors++;
                }


                // Filer out
                if (this.FilterDebug == false && le.Type == EntryType.Debug) {
                    continue;
                }

                if (this.FilterWarning == false && le.Type == EntryType.Warning) {
                    continue;
                }

                if (this.FilterNotice == false && (le.Type == EntryType.Notice || le.Type == EntryType.SuccessAudit)) {
                    continue;
                }

                if (this.FilterError == false && (le.Type == EntryType.Error || le.Type == EntryType.FailureAudit || le.Type == EntryType.Critical)) {
                    continue;
                }


                // Create and Add entry
                if (this.LogEntries.Count < limith) {
                    logEntryApp = new LogEntryApp() {
                        ActivityID = le.ActivityID,
                        Category = le.Category ?? "",
                        DateTimeStr = le.DateTime.ToString(),
                        MachineName = le.MachineName ?? "",
                        Message = le.Message ?? "",
                        SeqNumber = (long)le.Number,   // TODO: Number is a ulong
                        ServerName = le.ServerName ?? "",
                        Source = le.Source ?? "",
                        TypeStr = le.Type.ToString(),
                        UserName = le.UserName ?? ""
                    };
                    this.LogEntries.Add(logEntryApp);
                }

            }

        }

        private void ResetSummary() {
            this.Summary.Debug = 0;
            this.Summary.Notice = 0;
            this.Summary.Warning = 0;
            this.Summary.Errors = 0;
        }

    }



}