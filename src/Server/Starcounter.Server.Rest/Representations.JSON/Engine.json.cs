
using Starcounter;

namespace Starcounter.Server.Rest.Representations.JSON {
    using ExecutableReference = Engine.ExecutablesJson.ExecutingElementJson;

    /// <summary>
    /// Represents an engine resource.
    /// </summary>
    partial class Engine : Json {
        /// <summary>
        /// Gets an executable that match the given application file, if
        /// such is running inside the current database engine.
        /// </summary>
        /// <param name="applicationFile">The application file path of
        /// the executable being queried for.</param>
        /// <returns>An executable with the specified application file path,
        /// if such is running in the current database engine; <c>null</c>
        /// otherwise.</returns>
        public ExecutableReference GetExecutable(string applicationFile) {
            foreach (ExecutableReference exe in this.Executables.Executing) {
                if (exe.Path.Equals(applicationFile, System.StringComparison.InvariantCultureIgnoreCase)) {
                    return exe;
                }
            }
            return null;
        }
    }
}