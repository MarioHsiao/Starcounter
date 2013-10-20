
using Starcounter.Advanced;
using System;

namespace Starcounter {

    public class Configuration {
        public readonly static Configuration Current = new Configuration();
        public FileServerConfiguration FileServer = new FileServerConfiguration();
    }

    namespace Advanced {

        /// <summary>
        /// Configuration parameters for the static file server
        /// </summary>
        public class FileServerConfiguration {

            static Nullable<Boolean> CheckedCacheDisabled;

            /// <summary>
            /// Blocks both server side file caching and browser side file caching.
            /// Ideal when doing development with automatically refreshing pages.
            /// Defaults to false.
            /// <summary>
            public bool DisableAllCaching {
                get {

                    // TODO!!!!!!

                    if (CheckedCacheDisabled != null)
                        return CheckedCacheDisabled.Value;

                    CheckedCacheDisabled = new Boolean();
                    if (0 == String.Compare("True", Environment.GetEnvironmentVariable("SC_DISABLE_FILE_CACHING"), true))
                        CheckedCacheDisabled = true;

                    return CheckedCacheDisabled.Value;
                }
                set {
                    throw new NotImplementedException();
                }
            }

        }
    }
}
