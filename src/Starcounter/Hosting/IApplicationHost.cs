
namespace Starcounter.Hosting {
    /// <summary>
    /// By implementing this interface on the same class that defines the
    /// application entrypoint, applications are given an opportunity to access
    /// code host services just prior to the invocation of the entrypoint.
    /// The callback is invoked in a context where the code host assure there
    /// is no other application are booting too (as opposed to the entrypoint,
    /// which can run in parallel of applications are started in an async
    /// fashion).
    /// </summary>
    public interface IApplicationHost {
        /// <summary>
        /// Invoked by the code host when the given application are hosted but
        /// before it's entrypoint is invoked.
        /// </summary>
        /// <param name="codeHost">Reference to the code host</param>
        /// <param name="application">The application that is being booted.
        /// </param>
        void HostApplication(CodeHost codeHost, Application application);
    }
}
