
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ErrorHelpPages {
    /// <summary>
    /// Expose a simple Git API, based on a given command-line git
    /// implementation.
    /// </summary>
    public sealed class Git {
        public readonly string ExePath;
        public string LocalRepo;

        public event DataReceivedEventHandler ErrorDataReceived;
        public event DataReceivedEventHandler OutputDataReceived;

        public Git(string exePath) {
            if (string.IsNullOrEmpty(exePath)) {
                throw new ArgumentNullException("exePath");
            }
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException("The executable was not found", exePath);
            }
            this.ExePath = exePath;
            this.LocalRepo = Environment.CurrentDirectory;
        }

        public void Clone(string repository, string localDirectory = "", string options = "") {
            var args = new StringBuilder("clone ");
            if (!string.IsNullOrEmpty(options)) {
                args.Append(options);
                args.Append(" ");
            }
            args.Append(repository);
            if (!string.IsNullOrEmpty(localDirectory)) {
                args.Append(" ");
                args.Append(localDirectory);
            }

            Invoke(args.ToString());
        }

        public void Add(string file, params string[] arguments) {
            Environment.CurrentDirectory = LocalRepo;

            string args = string.Empty;
            if (arguments != null) {
                args = string.Join(" ", arguments);
            }

            Invoke(string.Concat("add ", file, " ", args));
        }

        public void Status(params string[] arguments) {
            Environment.CurrentDirectory = LocalRepo;

            string args = string.Empty;
            if (arguments != null) {
                args = string.Join(" ", arguments);
            }
            Invoke("status " + args);
        }

        public void Commit(params string[] arguments) {
            Environment.CurrentDirectory = LocalRepo;

            string args = string.Empty;
            if (arguments != null) {
                args = string.Join(" ", arguments);
            }

            Invoke(string.Concat("commit ", args));
        }

        public void Pull() {
            Environment.CurrentDirectory = LocalRepo;
            Invoke("pull");
        }

        public void Pull(string repository, string refspec = null, string options = "") {
            Environment.CurrentDirectory = LocalRepo;

            var args = new StringBuilder("pull ");
            if (!string.IsNullOrEmpty(options)) {
                args.Append(options);
                args.Append(" ");
            }
            args.Append(repository);
            if (!string.IsNullOrEmpty(refspec)) {
                args.Append(" ");
                args.Append(refspec);
            }

            Invoke(args.ToString());
        }

        public void Push(string repository, string refspec = null, string options = "") {
            Environment.CurrentDirectory = LocalRepo;

            var args = new StringBuilder("push ");
            if (!string.IsNullOrEmpty(options)) {
                args.Append(options);
                args.Append(" ");
            }
            args.Append(repository);
            if (!string.IsNullOrEmpty(refspec)) {
                args.Append(" ");
                args.Append(refspec);
            }

            Invoke(args.ToString());
        }

        public void Fetch(string repository, string refspec = null) {
            Environment.CurrentDirectory = LocalRepo;
            Invoke("fetch " + repository + " " + refspec ?? string.Empty);
        }

        void Invoke(string arguments, bool waitForExit = true) {
            var start = new ProcessStartInfo(ExePath, arguments);
            start.ErrorDialog = false;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardError = true;
            start.RedirectStandardOutput = true;
            var p = new Process { StartInfo = start };

            var output = new List<string>();
            var error = new List<string>();
            p.ErrorDataReceived += (sender, e) => {
                if (this.ErrorDataReceived != null) {
                    ErrorDataReceived(sender, e);
                }
            };
            p.OutputDataReceived += (sender, e) => {
                if (this.OutputDataReceived != null) {
                    OutputDataReceived(sender, e);
                }
            };

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            if (waitForExit) {
                p.WaitForExit();
            }

            if (p.ExitCode != 0) {
                var e = new ProcessExitException(p.ExitCode) {
                    ExeFileName = p.StartInfo.FileName,
                    Arguments = p.StartInfo.Arguments
                };
                throw e;
            }
        }
    }
}
