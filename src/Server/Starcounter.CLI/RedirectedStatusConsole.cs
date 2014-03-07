
namespace Starcounter.CLI {
    /// <summary>
    /// Implements the StatusConsole features in an environment where console
    /// output is redirected. In this environment, no status bar output should
    /// be written.
    /// </summary>
    /// <seealso cref="StatusConsole.Open"/>
    internal class RedirectedStatusConsole : StatusConsole {
        internal RedirectedStatusConsole() : base() {
        }

        internal override StatusConsole OpenConsole() {
            return this;
        }

        public override StatusConsole StartNewJob(string job) {
            return this;
        }

        public override StatusConsole CompleteJob(string result = null) {
            return this;
        }

        public override StatusConsole WriteTask(string task) {
            return this;
        }
    }
}