using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorHelpPages {

    public class ProcessExitException : Exception {
        public readonly int ExitCode;
        public string ExeFileName { get; set; }
        public string Arguments { get; set; }

        public ProcessExitException(int exitCode) {
            this.ExitCode = exitCode;
        }

        public override string Message {
            get {
                return ToString();
            }
        }

        public override string ToString() {
            return string.Format("{0} \"{1}\" exited with code {2}", Path.GetFileName(ExeFileName), Arguments, ExitCode);
        }
    }
}