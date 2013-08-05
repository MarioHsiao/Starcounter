
using Starcounter;

namespace Starcounter.Server.Rest.Representations.JSON {
    using ExecutableReference = Engine.ExecutablesObj.ExecutingObj;

    /// <summary>
    /// Represents an engine resource.
    /// </summary>
    partial class Engine : Json {

        public ExecutableReference GetExecutable(string exePath) {
            for (int i = 0; i < this.Executables.Executing.Count; i++) {
                var exe = this.Executables.Executing[i];
                if (exe.Path.Equals(exePath, System.StringComparison.InvariantCultureIgnoreCase)) {
                    return exe;
                }
            }
            return null;
        }
    }
}