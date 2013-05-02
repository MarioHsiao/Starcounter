
using Starcounter.Advanced;

namespace Starcounter.Server.Rest {
    /// <summary>
    /// Provides a set of extension methods for the <see cref="Response"/>
    /// class.
    /// </summary>
    /// <remarks>
    /// These are not server specific; consider moving them to a lower level
    /// in the hierarchy.
    /// </remarks>
    public static class ResponseExtensions {
        public static bool IsSuccessOr(this Response response, params int[] codes) {
            if (response.IsSuccessStatusCode)
                return true;

            foreach (var code in codes) {
                if (code == response.StatusCode)
                    return true;
            }

            return false;
        }

        public static bool IsAnyOf(this Response response, params int[] codes) {
            foreach (var code in codes) {
                if (code == response.StatusCode)
                    return true;
            }

            return false;
        }
    }
}
