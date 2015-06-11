using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Simple utility class that support a code sequence to run
    /// within the scope of a named application and then switch
    /// back to the (global) application scope of the previous
    /// application when the scope end.
    /// </summary>
    /// <example>
    /// // Either use 
    /// using (new AppScope("myapp")) { ... }
    /// // or
    /// AppScope.RunWithin("myapp", () => { ... });
    /// </example>
    internal sealed class AppScope : IDisposable {
        readonly string originalScope;

        /// <summary>
        /// Creates a new <see cref="AppScope"/> and switch the
        /// global application context to the named application.
        /// </summary>
        /// <param name="applicationName">The name of the application
        /// to scope.
        /// </param>
        public AppScope(string applicationName) {
            originalScope = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = applicationName;
        }

        /// <summary>
        /// Runs the given code within the scope of the named
        /// application.
        /// </summary>
        /// <param name="appName">Name of the application.</param>
        /// <param name="code">Code to run.</param>
        public static void RunWithin(string appName, Action code) {
            using (new AppScope(appName)) {
                code();
            }
        }

        void IDisposable.Dispose() {
            StarcounterEnvironment.AppName = originalScope;
        }
    }
}
