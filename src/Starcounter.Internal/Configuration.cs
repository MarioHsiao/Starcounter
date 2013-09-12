
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

            /// <summary>
            /// Blocks both server side file caching and browser side file caching.
            /// Ideal when doing development with automatically refreshing pages.
            /// Defaults to false.
            /// <summary>
            public bool DisableAllCaching {
                get {
                    // TODO!
                    return false;
                }
                set {
                    throw new NotImplementedException();
                }
            }

        }
    }
}
