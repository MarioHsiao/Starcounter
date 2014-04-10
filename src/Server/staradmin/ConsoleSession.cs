using Starcounter.CLI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace staradmin {

    internal sealed class ConsoleSession {
        ManualResetEvent stopped;
        CodeHostConsole[] consoles;

        private ConsoleSession(CodeHostConsole[] cons) {
            stopped = new ManualResetEvent(false);
            consoles = cons;
        }

        public static ConsoleSession StartNew(params CodeHostConsole[] consoles) {
            Trace.WriteLine("Console session starting");
            Trace.Assert(consoles != null && consoles.Length > 0);

            var all = new CodeHostConsole[consoles.Length];
            consoles.CopyTo(all, 0);

            return new ConsoleSession(all).OpenAll();
        }

        public void Stop() {
            Trace.WriteLine("Console session stopping");
            CloseAll();
            stopped.Set();
        }

        public void Wait() {
            Trace.WriteLine("Waiting for console session to stop");
            stopped.WaitOne();
        }

        ConsoleSession OpenAll() {
            foreach (var console in consoles) {
                console.Opened = OnConsoleOpened;
                console.Closed = OnConsoleClosed;
                console.MessageWritten = OnConsoleMessage;
                console.Open();
            }
            return this;
        }

        ConsoleSession CloseAll() {
            foreach (var console in consoles) {
                console.Close();
            }
            return this;
        }

        void OnConsoleOpened(CodeHostConsole console) {
            Trace.WriteLine(string.Format("Console \"{0}\" opened", console.DatabaseName));
        }

        void OnConsoleClosed(CodeHostConsole console) {
            Trace.WriteLine(string.Format("Console \"{0}\" closed", console.DatabaseName));
        }

        void OnConsoleMessage(CodeHostConsole console, string message) {
            Console.Write(message);
        }
    }
}
