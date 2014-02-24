using System;
using System.Collections.Generic;
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
            return new ConsoleSession();
        }

        public void Stop() {
            stopped.Set();
        }

        public void Wait() {
            stopped.WaitOne();
        }
    }
}
