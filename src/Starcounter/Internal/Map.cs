
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Temporary class providing global on/off for mapping.
    /// </summary>
    public static class MapConfig {

        /// <summary>
        /// Variable that stores decision if database mapping is enabled.
        /// </summary>
        static Boolean isMappingEnabled_ = ("True" == Environment.GetEnvironmentVariable("SC_ENABLE_MAPPING"));

        /// <summary>
        /// Indicates if mapping of applications should be enabled
        /// or not. Configuration will probably be on another level (app
        /// level?) in the final solution.
        /// </summary>
        public static bool Enabled {
            get {
                return isMappingEnabled_;
            }
        }
    }

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

            Self.POST(uri, null, null, null, 0, new HandlerOptions() { 
                ProxyDelegateTrigger = true,
                HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
            });

            //Console.WriteLine("MAP POST on {0}", uri);
        }

        /// <summary>
        /// Invoked when a instance of a database class is assigned to,
        /// i.e. mapping UPDATE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// updated.</param>
        public static void PUT(string uri) {

            Self.PUT(uri, null, null, null, 0, new HandlerOptions() {
                ProxyDelegateTrigger = true,
                HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
            });

            //Console.WriteLine("MAP PUT on {0}", uri);
        }

        /// <summary>
        /// Invoked when a instance of a database class is deleted,
        /// i.e. mapping DELETE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// deleted.</param>
        public static void DELETE(string uri) {

            Self.DELETE(uri, null, null, null, 0, new HandlerOptions() {
                ProxyDelegateTrigger = true,
                HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
            });

            //Console.WriteLine("MAP DELETE on {0}", uri);
        }
    }
}
