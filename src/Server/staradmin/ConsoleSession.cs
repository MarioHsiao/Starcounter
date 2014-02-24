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

        private ConsoleSession() {
            stopped = new ManualResetEvent(false);
        }

        public static ConsoleSession StartNew(params CodeHostConsole[] consoles) {
            Trace.WriteLine("Trace session starting");
            Trace.Assert(consoles != null && consoles.Length > 0);

            return new ConsoleSession();
        }

        public void Stop() {
            Trace.WriteLine("Trace session stopping");
            stopped.Set();
        }

        public void Wait() {
            Trace.WriteLine("Waiting for trace session to stop");
            stopped.WaitOne();
        }
    }
}
