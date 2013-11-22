
using Starcounter;

namespace Starcounter.Server.Rest.Representations.JSON {
    using ExecutableReference = Engine.ExecutablesJson.ExecutingElementJson;

    /// <summary>
    /// Represents an engine resource.
    /// </summary>
    partial class Engine : Json {

        public ExecutableReference GetExecutable(string exePath) {
			foreach (ExecutableReference exe in this.Executables.Executing){
                if (exe.Path.Equals(exePath, System.StringComparison.InvariantCultureIgnoreCase)) {
                    return exe;
                }
            }
            return null;
        }
    }
}