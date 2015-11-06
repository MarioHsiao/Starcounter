
namespace Starcounter.Legacy {
    /// <summary>
    /// Host extension methods for legacy applications that need to
    /// access a legacy context.
    /// </summary>
    public static class LegacyHostExtensions {
        /// <summary>
        /// Gets the legacy context of the given application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A <see cref="LegacyContext"/> for the given
        /// application.</returns>
        public static LegacyContext GetLegacyContext(this Application application) {
            return LegacyContext.GetContext(application);
        }
    }
}