
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

        /// <summary>
        /// Gets an application by its name.
        /// </summary>
        /// <param name="applicationName">The name of the application to
        /// look for and retrive.</param>
        /// <returns>The application with the given name, or <c>null</c>
        /// if not found.</returns>
        public ExecutableReference GetApplicationByName(string applicationName) {
            foreach (ExecutableReference exe in this.Executables.Executing) {
                if (exe.Name.Equals(applicationName, System.StringComparison.InvariantCultureIgnoreCase)) {
                    return exe;
                }
            }
            return null;
        }
    }
}