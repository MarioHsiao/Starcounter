
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Temporary class providing global on/off for mapping.
    /// </summary>
    public static class MapConfig {

        /// <summary>
        /// Global mapping flag.
        /// </summary>
        public static Boolean IsGlobalMappingEnabled;

        /// <summary>
        /// Variable that stores decision if database mapping is enabled.
        /// </summary>
        [ThreadStatic]
        static Nullable<Boolean> isMappingEnabled_;

        /// <summary>
        /// Indicates if mapping of applications should be enabled
        /// or not. Configuration will probably be on another level (app
        /// level?) in the final solution.
        /// </summary>
        public static bool Enabled {
            get {
                if (null == isMappingEnabled_) {
                    isMappingEnabled_ = IsGlobalMappingEnabled;
                }

                return isMappingEnabled_.Value;
            }
            set {
                isMappingEnabled_ = value;
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

            if (MapConfig.Enabled) {

                StarcounterEnvironment.RunWithinApplication(null, () => {

                    Self.POST(uri, null, null, null, 0, new HandlerOptions() {
                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                    });

                });
            }

        }

        /// <summary>
        /// Invoked when a instance of a database class is assigned to,
        /// i.e. mapping UPDATE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// updated.</param>
        public static void PUT(string uri) {

            if (MapConfig.Enabled) {

                StarcounterEnvironment.RunWithinApplication(null, () => {

                    Self.PUT(uri, null, null, null, 0, new HandlerOptions() {
                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                    });
                });
            }
        }

        /// <summary>
        /// Invoked when a instance of a database class is deleted,
        /// i.e. mapping DELETE.
        /// </summary>
        /// <param name="uri">The unique mapping URI of the object being
        /// deleted.</param>
        public static void DELETE(string uri) {

            if (MapConfig.Enabled) {

                StarcounterEnvironment.RunWithinApplication(null, () => {

                    Self.DELETE(uri, null, null, null, 0, new HandlerOptions() {
                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                    });
                });
            }
        }
    }
}
