
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.CLI {
    /// <summary>
    /// Manage the output of the star.exe command and it's
    /// shell bootstrapper counterpart.
    /// </summary>
    public sealed class StarConsole {
        readonly int cursorLeft;
        readonly int cursorTop;
        readonly int lines;
        string currentJob;

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

        /// <summary>
        /// Gets or sets the console color used to display output
        /// routed from a starting/stopping application.
        /// </summary>
        public ConsoleColor ApplicationOutputColor {
            get;
            set;
        }

        private StarConsole(int lineCount, int left, int top) {
            lines = lineCount;
            cursorLeft = left;
            cursorTop = top;
            ProgressColor = ConsoleColor.DarkGray;
            CompletionColor = ConsoleColor.DarkGreen;
            ApplicationOutputColor = Console.ForegroundColor;
        }

        /// <summary>
        /// Opens a new the console at the current position of the
        /// underying system console.
        /// </summary>
        /// <param name="lines">The number of lines to reserve for the
        /// console being opened; single (1) line if not given.</param>
        /// <returns>A new console.</returns>
        public static StarConsole Open(int lines = 1) {
            if (lines < 1) throw new ArgumentOutOfRangeException();
            var console = new StarConsole(lines, Console.CursorLeft, Console.CursorTop);
            while (lines-- > 0) Console.WriteLine();
            return console;
        }

        /// <summary>
        /// Starts a job in the current console.
        /// </summary>
        /// <param name="job">The description of the job being started.</param>
        /// <returns>Reference to self.</returns>
        public StarConsole StartNewJob(string job) {
            if (string.IsNullOrEmpty(job)) {
                throw new ArgumentNullException("job");
            }
            currentJob = job;
            return Write(job, string.Empty, ProgressColor);
        }

        /// <summary>
        /// Completes the job bound to the current console.
        /// </summary>
        /// <param name="result">An optional result to display.</param>
        /// <returns>Reference to self.</returns>
        public StarConsole CompleteJob(string result = null) {
            result = result ?? string.Empty;
            Write(currentJob, result, CompletionColor);
            currentJob = null;
            return this;
        }

        /// <summary>
        /// Instructs the current console to display a message that
        /// a new task is ongoing.
        /// </summary>
        /// <param name="task">The task that is starting.</param>
        /// <returns>Reference to self.</returns>
        public StarConsole WriteTask(string task) {
            return Write(currentJob, task, ProgressColor);
        }
        StarConsole Write(string job, string taskOrResult, ConsoleColor color) {
            int left, top;
            left = Console.CursorLeft;
            top = Console.CursorTop;

            try {
                var content = string.Format("{0} {1}", currentJob, taskOrResult);
                content = content.PadRight(Console.WindowWidth);

                Console.SetCursorPosition(cursorLeft, cursorTop);
                ConsoleUtil.ToConsoleWithColor(content, color);

            } finally {
                Console.SetCursorPosition(left, top);
            }

            return this;
        }
    }
}
