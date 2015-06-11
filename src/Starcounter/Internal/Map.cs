
using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Implements the actual handler layer of the mapping functionality.
    /// </summary>
    internal static class Map {
        /// <summary>
        /// Invoked when a new instance of a database class is created,
        /// i.e. mapping INSERT.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// created.</param>
        public static void POST(string uri) {
            // Implement logic
            // TODO:

            Console.WriteLine("MAP POST on {0}", uri);
        }

        /// <summary>
        /// Invoked when a instance of a database class is assigned to,
        /// i.e. mapping UPDATE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// updated.</param>
        public static void PUT(string uri) {
            // Implement logic
            // TODO:

            Console.WriteLine("MAP PUT on {0}", uri);
        }

        /// <summary>
        /// Invoked when a instance of a database class is deleted,
        /// i.e. mapping DELETE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// deleted.</param>
        public static void DELETE(string uri) {
            // Implement logic
            // TODO:

            Console.WriteLine("MAP DELETE on {0}", uri);
        }
    }
}
