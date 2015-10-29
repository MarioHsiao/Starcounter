
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.CLI {
    /// <summary>
    /// Manage the output of the star.exe command and it's
    /// shell bootstrapper counterpart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An instance of this class represents an area of the console,
    /// normally a single line, where a hosting application write
    /// output about an onging job, and intermediate tasks. It's
    /// comparable to the Visual Studio status bar in that output
    /// that is written to it is written on a fixed surface and will
    /// overwrite any previous content.
    /// </para>
    /// <para>
    /// This class is not thread-safe.
    /// </para>
    /// </remarks>
    public class StatusConsole {
        readonly int cursorLeft;
        readonly int cursorTop;
        readonly int lines;
        string currentJob;
        string currentTask;
        PulseTimer timer;
        DateTime? latestUpdate;
        object writeLock;
        
        private class PulseTimer {
            const int interval = 750;
            readonly StatusConsole console;
            Timer timer;
            string dots;
            DateTime? lastWrite;

            public PulseTimer(StatusConsole c) {
                console = c;
                dots = " .";
                timer = new Timer(OnPulseProgressTimer, c, 0, interval);
            }

            public void Dispose() {
                timer.Dispose();
            }

            void OnPulseProgressTimer(object state) {
                StatusConsole console = (StatusConsole)state;
                lock (console.writeLock) {
                    var latest = console.latestUpdate;
                    if (latest.HasValue) {
                        if (!lastWrite.HasValue) {
                            lastWrite = latest.Value;
                        } else {
                            if (lastWrite == latest.Value) {
                                dots = dots.Length == 4 ? " ." : dots + ".";
                                console.ExclusiveWrite(
                                    console.currentJob,
                                    console.currentTask, 
                                    dots,
                                    console.ProgressColor
                                );
                            }
                            lastWrite = console.latestUpdate.Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the console color used to display output
        /// about a job/task in progress.
        /// </summary>
        public ConsoleColor ProgressColor {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the console color used to display output
        /// about a job that has completed.
        /// </summary>
        public ConsoleColor CompletionColor {
            get;
            set;
        }

        internal StatusConsole() {
        }

        private StatusConsole(int lineCount, int left, int top) {
            lines = lineCount;
            cursorLeft = left;
            cursorTop = top;
            ProgressColor = ConsoleColor.DarkGray;
            CompletionColor = ConsoleColor.DarkGreen;
            writeLock = new object();
        }

        /// <summary>
        /// Opens a new the console at the current position of the
        /// underying system console.
        /// </summary>
        /// <param name="lines">The number of lines to reserve for the
        /// console being opened; single (1) line if not given.</param>
        /// <returns>A new console.</returns>
        public static StatusConsole Open(int lines = 1) {
            if (lines < 1) throw new ArgumentOutOfRangeException();

            StatusConsole console;
            console =
                Console.IsOutputRedirected || Console.IsInputRedirected || Console.IsErrorRedirected 
                ? new RedirectedStatusConsole()
                : new StatusConsole(lines, Console.CursorLeft, Console.CursorTop);

            return console.OpenConsole();
        }

        internal virtual StatusConsole OpenConsole() {
            var l = lines;
            while (l-- > 0) Console.WriteLine();
            return this;
        }

        /// <summary>
        /// Starts a job in the current console.
        /// </summary>
        /// <param name="job">The description of the job being started.</param>
        /// <returns>Reference to self.</returns>
        public virtual StatusConsole StartNewJob(string job) {
            if (string.IsNullOrEmpty(job)) {
                throw new ArgumentNullException("job");
            } else if (currentJob != null) {
                CompleteJob();
            }

            currentJob = job;
            Write(job, string.Empty, ProgressColor);

            timer = new PulseTimer(this);
            return this;
        }

        /// <summary>
        /// Completes the job bound to the current console.
        /// </summary>
        /// <param name="result">An optional result to display.</param>
        /// <returns>Reference to self.</returns>
        public virtual StatusConsole CompleteJob(string result = null) {
            result = result ?? string.Empty;
            Write(currentJob, result, CompletionColor);
            timer.Dispose();
            timer = null;
            currentJob = null;
            currentTask = null;
            return this;
        }

        /// <summary>
        /// Completes the job bound to the current console after first
        /// chaging it to <paramref name="job"/>.
        /// </summary>
        /// <param name="job">The final job, now being completed.</param>
        /// <param name="result">An optional result to display.</param>
        /// <returns>Reference to self.</returns>
        public virtual StatusConsole CompleteWithFinalJob(string job, string result = null) {
            currentJob = job ?? string.Empty;
            return CompleteJob(result);
        }

        /// <summary>
        /// Instructs the current console to display a message that
        /// a new task is ongoing.
        /// </summary>
        /// <param name="task">The task that is starting.</param>
        /// <returns>Reference to self.</returns>
        public virtual StatusConsole WriteTask(string task) {
            currentTask = task;
            return Write(currentJob, task, ProgressColor);
        }

        StatusConsole Write(string job, string taskOrResult, ConsoleColor color) {
            lock (writeLock) {
                ExclusiveWrite(job, taskOrResult, color: color);
                return this;
            }
        }

        StatusConsole ExclusiveWrite(string job, string taskOrResult, string pulse = null, ConsoleColor? color = null) {
            var col = color.HasValue ? color.Value : ProgressColor;
            int left, top;
            var output = new StringBuilder();

            left = Console.CursorLeft;
            top = Console.CursorTop;
            pulse = pulse ?? string.Empty;

            output.Append(job);
            if (!string.IsNullOrEmpty(taskOrResult)) {
                output.Append(" (");
                output.Append(taskOrResult);
            }
            output.Append(pulse);
            if (!string.IsNullOrEmpty(taskOrResult)) {
                output.Append(")");
            }

            try {
                latestUpdate = DateTime.Now;

                var content = output.ToString();
                var maxLength = Console.WindowWidth * lines;
                
                if (content.Length > maxLength) {
                    content = content.Substring(0, maxLength);
                } else {
                    content = content.PadRight(maxLength);
                }

                Console.SetCursorPosition(cursorLeft, cursorTop);
                ConsoleUtil.ToConsoleWithColor(content, col);

            } finally {
                Console.SetCursorPosition(left, top);
            }

            return this;
        }
    }
}
